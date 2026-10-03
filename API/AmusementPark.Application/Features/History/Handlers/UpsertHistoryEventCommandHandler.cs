using System.Globalization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class UpsertHistoryEventCommandHandler : ICommandHandler<UpsertHistoryEventCommand, ApplicationResult<HistoryEvent>>
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IStandaloneAttractionRepository? standaloneAttractionRepository;
    private readonly HistoricalCanonicalResourceRetractionService canonicalResourceRetractionService;
    private readonly IHistoricalNarrativeCanonicalizer historicalNarrativeCanonicalizer;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public UpsertHistoryEventCommandHandler(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        HistoricalCanonicalResourceRetractionService canonicalResourceRetractionService,
        IHistoricalNarrativeCanonicalizer historicalNarrativeCanonicalizer,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
        : this(
            historyEventRepository,
            parkRepository,
            parkItemRepository,
            null,
            canonicalResourceRetractionService,
            historicalNarrativeCanonicalizer,
            sitemapRefreshScheduler)
    {
    }

    public UpsertHistoryEventCommandHandler(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IStandaloneAttractionRepository? standaloneAttractionRepository,
        HistoricalCanonicalResourceRetractionService canonicalResourceRetractionService,
        IHistoricalNarrativeCanonicalizer historicalNarrativeCanonicalizer,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.standaloneAttractionRepository = standaloneAttractionRepository;
        this.canonicalResourceRetractionService = canonicalResourceRetractionService
            ?? throw new ArgumentNullException(nameof(canonicalResourceRetractionService));
        this.historicalNarrativeCanonicalizer = historicalNarrativeCanonicalizer
            ?? throw new ArgumentNullException(nameof(historicalNarrativeCanonicalizer));
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
    }

    public async Task<ApplicationResult<HistoryEvent>> HandleAsync(UpsertHistoryEventCommand command, CancellationToken cancellationToken = default)
    {
        ApplicationError? validationError = await this.ValidateAsync(command.Event, cancellationToken);
        if (validationError is not null)
        {
            return ApplicationResult<HistoryEvent>.Failure(validationError);
        }

        string ownerId = command.Event.OwnerId?.Trim() ?? string.Empty;
        string key = NormalizeKey(command.Event.Key) ?? BuildFallbackKey(command.Event);
        HistoryEvent? existing = !string.IsNullOrWhiteSpace(command.Event.Id)
            ? await this.historyEventRepository.GetByIdAsync(command.Event.Id.Trim(), true, cancellationToken)
            : await this.historyEventRepository.GetByOwnerKeyAsync(command.Event.EntityType, ownerId, key, cancellationToken);

        HistoryEvent historyEvent = existing is null
            ? new HistoryEvent()
            : existing;
        DateTime expectedUpdatedAtUtc = existing?.UpdatedAtUtc ?? default;
        Guid? expectedCanonicalFactId = existing?.CanonicalFactId;
        HistoricalCanonicalResourceRetractionSnapshot? retractionSnapshot = null;

        if (existing?.CanonicalFactId is Guid canonicalFactId)
        {
            retractionSnapshot = await this.canonicalResourceRetractionService.RetractAsync(
                canonicalFactId,
                cancellationToken);
        }

        this.ApplyWriteModel(historyEvent, command.Event, ownerId, key);

        HistoryEvent saved;
        if (existing is null)
        {
            saved = await this.historyEventRepository.CreateAsync(historyEvent, cancellationToken);
        }
        else
        {
            Guid mutationId = Guid.NewGuid();
            HistoryEvent? updatedHistoryEvent;
            try
            {
                updatedHistoryEvent = await this.historyEventRepository.UpdateAsync(
                    historyEvent.Id,
                    historyEvent,
                    expectedUpdatedAtUtc,
                    expectedCanonicalFactId,
                    mutationId,
                    cancellationToken);
            }
            catch (Exception mutationException)
            {
                updatedHistoryEvent = await HistoricalNarrativeMutationRecovery.ResolveCommittedUpdateAsync(
                    this.historyEventRepository,
                    this.canonicalResourceRetractionService,
                    historyEvent.Id,
                    mutationId,
                    expectedUpdatedAtUtc,
                    expectedCanonicalFactId,
                    retractionSnapshot,
                    mutationException);
                if (updatedHistoryEvent is null)
                {
                    throw;
                }
            }

            if (updatedHistoryEvent is null)
            {
                InvalidOperationException conflictException = new InvalidOperationException(
                    "The historical narrative changed concurrently and could not be updated safely.");
                await this.RestorePreviousResourcesIfNeededAsync(
                    historyEvent.Id,
                    expectedUpdatedAtUtc,
                    expectedCanonicalFactId,
                    retractionSnapshot,
                    conflictException);
                throw conflictException;
            }

            saved = updatedHistoryEvent;
        }
        HistoricalNarrativeCanonicalizationResult canonicalization;
        try
        {
            canonicalization = await this.historicalNarrativeCanonicalizer.CanonicalizeAsync(
                saved,
                cancellationToken);
        }
        catch (Exception canonicalizationException)
        {
            await this.RecoverCanonicalizationFailureAsync(
                saved,
                expectedCanonicalFactId,
                retractionSnapshot,
                canonicalizationException);
            throw;
        }
        if (canonicalization.State == HistoricalNarrativeCanonicalizationState.Blocked)
        {
            return ApplicationResult<HistoryEvent>.Failure(
                HistoryApplicationErrors.InvalidEventType());
        }

        await HistoricalNarrativeCanonicalLinker.LinkAsync(
            this.historyEventRepository,
            this.canonicalResourceRetractionService,
            saved,
            canonicalization,
            cancellationToken);

        saved.CanonicalFactId = canonicalization.CanonicalFactId;
        saved.CanonicalizationState = canonicalization.State;

        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult<HistoryEvent>.Success(saved);
    }

    private async Task RecoverCanonicalizationFailureAsync(
        HistoryEvent saved,
        Guid? expectedCanonicalFactId,
        HistoricalCanonicalResourceRetractionSnapshot? retractionSnapshot,
        Exception canonicalizationException)
    {
        List<Exception> recoveryExceptions = new List<Exception>();
        Guid generatedFactId = HistoricalNarrativeCanonicalIdentity.CreateGuid(
            HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
            "fact",
            saved.Id,
            saved.UpdatedAtUtc);
        Guid[] generatedSourceIds = saved.Sources
            .Select((_, index) => HistoricalNarrativeCanonicalIdentity.CreateGuid(
                HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
                "source",
                saved.Id,
                saved.UpdatedAtUtc,
                index))
            .ToArray();
        try
        {
            await this.canonicalResourceRetractionService.RetractGeneratedAsync(
                generatedFactId,
                generatedSourceIds,
                CancellationToken.None);
        }
        catch (Exception cleanupException)
        {
            recoveryExceptions.Add(cleanupException);
        }

        try
        {
            await this.RestorePreviousResourcesIfNeededAsync(
                saved.Id,
                saved.UpdatedAtUtc,
                expectedCanonicalFactId,
                retractionSnapshot,
                canonicalizationException);
        }
        catch (Exception restorationException)
        {
            recoveryExceptions.Add(restorationException);
        }

        if (recoveryExceptions.Count > 0)
        {
            throw new AggregateException(
                "The failed canonical HIST update could not be fully compensated.",
                new[] { canonicalizationException }.Concat(recoveryExceptions));
        }
    }

    private Task RestorePreviousResourcesIfNeededAsync(
        string historyEventId,
        DateTime expectedUpdatedAtUtc,
        Guid? expectedCanonicalFactId,
        HistoricalCanonicalResourceRetractionSnapshot? retractionSnapshot,
        Exception mutationFailure)
    {
        if (!expectedCanonicalFactId.HasValue || retractionSnapshot is null)
        {
            return Task.CompletedTask;
        }

        return HistoricalNarrativeMutationRecovery.RestoreIfNarrativeIsUnchangedAsync(
            this.historyEventRepository,
            this.canonicalResourceRetractionService,
            historyEventId,
            expectedUpdatedAtUtc,
            expectedCanonicalFactId.Value,
            retractionSnapshot,
            mutationFailure);
    }

    private async Task<ApplicationError?> ValidateAsync(HistoryEventWriteModel model, CancellationToken cancellationToken)
    {
        string ownerId = model.OwnerId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return HistoryApplicationErrors.InvalidOwner();
        }

        if (model.Year <= 0 || model.Month is < 1 or > 12 || model.Day is < 1 or > 31)
        {
            return HistoryApplicationErrors.InvalidDate();
        }

        if (model.DatePrecision == HistoryDatePrecision.Month && !model.Month.HasValue)
        {
            return HistoryApplicationErrors.InvalidDate();
        }

        if (model.DatePrecision == HistoryDatePrecision.Day && (!model.Month.HasValue || !model.Day.HasValue))
        {
            return HistoryApplicationErrors.InvalidDate();
        }

        if (HistoricalNarrativeTypeMapper.RequiresManualClassification(
                model.EntityType,
                model.EventType)
            || !HistoricalNarrativeTypeMapper.TryMap(
                model.EntityType,
                model.EventType,
                out HistoricalNarrativeTypeMapping? mapping)
            || mapping is null)
        {
            return HistoryApplicationErrors.InvalidEventType();
        }

        HistoryEvent canonicalCandidate = new HistoryEvent();
        this.ApplyWriteModel(
            canonicalCandidate,
            model,
            ownerId,
            NormalizeKey(model.Key) ?? BuildFallbackKey(model));
        try
        {
            HistoricalNarrativeCanonicalFactFactory.BuildPeriod(canonicalCandidate);
        }
        catch (HistoricalTemporalValidationException)
        {
            return HistoryApplicationErrors.InvalidDate();
        }

        if (mapping.AttributeKind.HasValue
            && HistoricalNarrativeCanonicalFactFactory.BuildStructuredValue(
                canonicalCandidate,
                mapping) is null)
        {
            return HistoryApplicationErrors.InvalidCanonicalShape();
        }

        bool hasValidSource = HistoricalNarrativeCanonicalSourcePlanner.HasValidSource(
            canonicalCandidate);
        if (mapping.FactType == HistoricalFactType.Other && !hasValidSource)
        {
            return HistoryApplicationErrors.InvalidCanonicalShape();
        }

        if (canonicalCandidate.IsVisible && !hasValidSource)
        {
            return HistoryApplicationErrors.MissingPublicationSource();
        }

        if (model.EntityType == HistoryEntityType.Park)
        {
            if (!Enum.TryParse(model.EventType, true, out ParkHistoryEventType _))
            {
                return HistoryApplicationErrors.InvalidEventType();
            }

            Park? park = await this.parkRepository.GetByIdAsync(ownerId, true, cancellationToken);
            return park is null ? HistoryApplicationErrors.InvalidOwner() : null;
        }

        if (!Enum.TryParse(model.EventType, true, out ParkItemHistoryEventType _))
        {
            return HistoryApplicationErrors.InvalidEventType();
        }

        if (model.EntityType == HistoryEntityType.StandaloneAttraction)
        {
            if (this.standaloneAttractionRepository is null)
            {
                return HistoryApplicationErrors.InvalidOwner();
            }

            StandaloneAttraction? attraction = await this.standaloneAttractionRepository.GetByIdAsync(ownerId, true, cancellationToken);
            return attraction is null ? HistoryApplicationErrors.InvalidOwner() : null;
        }

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(ownerId, true, cancellationToken);
        return item is null ? HistoryApplicationErrors.InvalidOwner() : null;
    }

    private void ApplyWriteModel(HistoryEvent historyEvent, HistoryEventWriteModel model, string ownerId, string key)
    {
        historyEvent.Key = key;
        historyEvent.EntityType = model.EntityType;
        historyEvent.OwnerId = ownerId;
        historyEvent.ParkId = NormalizeId(model.EntityType == HistoryEntityType.Park ? ownerId : model.ParkId);
        historyEvent.ParkItemId = NormalizeId(model.EntityType == HistoryEntityType.ParkItem ? ownerId : model.ParkItemId);
        historyEvent.ContextParkId = NormalizeId(model.ContextParkId ?? historyEvent.ParkId);
        historyEvent.Year = model.Year;
        historyEvent.Month = model.DatePrecision == HistoryDatePrecision.Year ? null : model.Month;
        historyEvent.Day = model.DatePrecision == HistoryDatePrecision.Day ? model.Day : null;
        historyEvent.DatePrecision = model.DatePrecision;
        historyEvent.EventType = model.EventType.Trim();
        historyEvent.IsMajor = model.IsMajor;
        historyEvent.IsVisible = model.IsVisible;
        historyEvent.Slug = NormalizeKey(model.Slug);
        historyEvent.Titles = model.Titles.ToList();
        historyEvent.Summaries = model.Summaries.ToList();
        historyEvent.MainImageId = NormalizeId(model.MainImageId);
        historyEvent.PreviousName = NormalizeNullable(model.PreviousName);
        historyEvent.NewName = NormalizeNullable(model.NewName);
        historyEvent.PreviousLogoImageId = NormalizeId(model.PreviousLogoImageId);
        historyEvent.NewLogoImageId = NormalizeId(model.NewLogoImageId);
        historyEvent.PreviousOperatorId = NormalizeId(model.PreviousOperatorId);
        historyEvent.NewOperatorId = NormalizeId(model.NewOperatorId);
        historyEvent.LocationLabel = NormalizeNullable(model.LocationLabel);
        historyEvent.RelatedParkIds = NormalizeIds(model.RelatedParkIds);
        historyEvent.RelatedParkItemIds = NormalizeIds(model.RelatedParkItemIds);
        historyEvent.Sources = model.Sources.Where(static source => !string.IsNullOrWhiteSpace(source.Url)).ToList();
        historyEvent.Article = model.IsMajor ? model.Article : null;

        if (historyEvent.Article is not null && string.IsNullOrWhiteSpace(historyEvent.Article.Slug))
        {
            historyEvent.Article.Slug = historyEvent.Slug;
        }
    }

    private static string BuildFallbackKey(HistoryEventWriteModel model)
    {
        string title = model.Titles.FirstOrDefault(static text => !string.IsNullOrWhiteSpace(text.Value))?.Value ?? model.EventType;
        string month = model.Month.HasValue ? model.Month.Value.ToString("00", CultureInfo.InvariantCulture) : "00";
        string day = model.Day.HasValue ? model.Day.Value.ToString("00", CultureInfo.InvariantCulture) : "00";
        return $"{model.EntityType}-{model.Year.ToString(CultureInfo.InvariantCulture)}-{month}-{day}-{title}".ToLowerInvariant();
    }

    private static string? NormalizeKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeId(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static List<string> NormalizeIds(IEnumerable<string> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
