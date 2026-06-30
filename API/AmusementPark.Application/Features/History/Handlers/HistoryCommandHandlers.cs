using System.Globalization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class UpsertHistoryEventCommandHandler : ICommandHandler<UpsertHistoryEventCommand, ApplicationResult<HistoryEvent>>
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public UpsertHistoryEventCommandHandler(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
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

        this.ApplyWriteModel(historyEvent, command.Event, ownerId, key);

        HistoryEvent saved = existing is null
            ? await this.historyEventRepository.CreateAsync(historyEvent, cancellationToken)
            : await this.historyEventRepository.UpdateAsync(historyEvent.Id, historyEvent, cancellationToken) ?? historyEvent;

        return ApplicationResult<HistoryEvent>.Success(saved);
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

public sealed class DeleteHistoryEventCommandHandler : ICommandHandler<DeleteHistoryEventCommand, ApplicationResult>
{
    private readonly IHistoryEventRepository historyEventRepository;

    public DeleteHistoryEventCommandHandler(IHistoryEventRepository historyEventRepository)
    {
        this.historyEventRepository = historyEventRepository;
    }

    public async Task<ApplicationResult> HandleAsync(DeleteHistoryEventCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.EventId))
        {
            return ApplicationResult.Failure(ApplicationErrors.Required("eventId"));
        }

        bool deleted = await this.historyEventRepository.DeleteAsync(command.EventId.Trim(), cancellationToken);
        return deleted
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(ApplicationErrors.EntityNotFound(nameof(HistoryEvent), command.EventId));
    }
}
