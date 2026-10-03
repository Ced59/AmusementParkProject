using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Tests.Features.History.Services;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

internal sealed class HistoryUpsertTestContext
{
    private readonly Mock<IParkRepository> parkRepository;
    private readonly Mock<IParkGraphUpsertHistoryRepository> upsertHistoryRepository;
    private readonly Mock<ISearchProjectionWriter> searchProjectionWriter;
    private readonly Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier;
    private HistoryEvent persistedEvent;
    private Exception? nextHistoryUpdateFailure;

    public HistoryUpsertTestContext(HistoryEvent existingEvent)
    {
        this.persistedEvent = CloneHistoryEvent(existingEvent);
        this.parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        this.parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(static () => BuildPark());
        this.parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, Park park, CancellationToken _) => park);

        this.HistoryEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        this.HistoricalFactRepository = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        this.HistoricalNarrativeCanonicalizer =
            new Mock<IHistoricalNarrativeCanonicalizer>(MockBehavior.Strict);
        this.HistoryEventRepository
            .Setup(value => value.GetByOwnerKeyAsync(
                HistoryEntityType.Park,
                "park-1",
                "history-1979",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CloneHistoryEvent(this.persistedEvent));
        this.HistoryEventRepository
            .Setup(value => value.GetByIdAsync(
                "history-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CloneHistoryEvent(this.persistedEvent));
        this.HistoryEventRepository
            .Setup(value => value.UpdateAsync(
                "history-1",
                It.IsAny<HistoryEvent>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, HistoryEvent historyEvent, DateTime _, Guid? _, Guid _, CancellationToken _) =>
            {
                if (this.nextHistoryUpdateFailure is not null)
                {
                    Exception failure = this.nextHistoryUpdateFailure;
                    this.nextHistoryUpdateFailure = null;
                    return Task.FromException<HistoryEvent?>(failure);
                }

                this.persistedEvent = CloneHistoryEvent(historyEvent);
                return Task.FromResult<HistoryEvent?>(CloneHistoryEvent(historyEvent));
            });
        this.HistoryEventRepository
            .Setup(value => value.GetCommittedUpdateAsync(
                "history-1",
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent?)null);
        this.HistoricalNarrativeCanonicalizer
            .Setup(value => value.NeedsCanonicalRepairAsync(
                It.IsAny<HistoryEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        this.HistoricalNarrativeCanonicalizer
            .Setup(value => value.CanonicalizeAsync(
                It.IsAny<HistoryEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent historyEvent, CancellationToken _) =>
                new HistoricalNarrativeCanonicalizationResult(
                    Guid.NewGuid(),
                    HistoricalNarrativeCanonicalizationState.Canonicalized,
                    Array.Empty<string>()));
        this.HistoryEventRepository
            .Setup(value => value.SetCanonicalizationAsync(
                "history-1",
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                HistoricalNarrativeCanonicalizationState.Canonicalized,
                HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, Guid?, HistoricalNarrativeCanonicalizationState, string, IReadOnlyCollection<string>, CancellationToken>(
                (_, _, canonicalFactId, state, _, _, _) =>
                {
                    this.persistedEvent.CanonicalFactId = canonicalFactId;
                    this.persistedEvent.CanonicalizationState = state;
                })
            .ReturnsAsync(true);

        this.upsertHistoryRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        this.upsertHistoryRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        this.searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        this.searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        this.publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        this.publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        this.Processor = new ParkGraphUpsertProcessor(
            this.parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            this.searchProjectionWriter.Object,
            this.upsertHistoryRepository.Object,
            this.publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance,
            historyEventRepository: this.HistoryEventRepository.Object,
            canonicalResourceRetractionService:
                HistoricalCanonicalResourceRetractionServiceTestFactory.Create(this.HistoricalFactRepository.Object),
            historicalNarrativeCanonicalizer: this.HistoricalNarrativeCanonicalizer.Object);
    }

    public Mock<IHistoryEventRepository> HistoryEventRepository { get; }

    public Mock<IHistoricalFactRepository> HistoricalFactRepository { get; }

    public Mock<IHistoricalNarrativeCanonicalizer> HistoricalNarrativeCanonicalizer { get; }

    private ParkGraphUpsertProcessor Processor { get; }

    public async Task<ApplicationResult<ParkGraphUpsertResult>> PreviewAsync(string rawJson)
    {
        return await this.ProcessAsync(rawJson, false);
    }

    public async Task<ApplicationResult<ParkGraphUpsertResult>> ApplyAsync(string rawJson)
    {
        return await this.ProcessAsync(rawJson, true);
    }

    public HistoryEvent ReadPersistedEvent()
    {
        return CloneHistoryEvent(this.persistedEvent);
    }

    public void FailNextHistoryUpdate(Exception failure)
    {
        this.nextHistoryUpdateFailure = failure
            ?? throw new ArgumentNullException(nameof(failure));
    }

    public void FailCanonicalization(Exception failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        this.HistoricalNarrativeCanonicalizer
            .Setup(value => value.CanonicalizeAsync(
                It.IsAny<HistoryEvent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
    }

    public void SetCanonicalRepairRequired(bool repairRequired)
    {
        this.HistoricalNarrativeCanonicalizer
            .Setup(value => value.NeedsCanonicalRepairAsync(
                It.IsAny<HistoryEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(repairRequired);
        if (!repairRequired || !this.persistedEvent.CanonicalFactId.HasValue)
        {
            return;
        }

        Guid canonicalFactId = this.persistedEvent.CanonicalFactId.Value;
        HistoricalFact currentFact = CreateRepairableCanonicalFact(canonicalFactId);
        this.HistoricalFactRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                canonicalFactId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentFact);
        this.HistoricalFactRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id == canonicalFactId
                    && fact.PublicationState == HistoricalPublicationState.Withdrawn),
                It.Is<HistoricalReviewEvent>(review =>
                    review.EventType == HistoricalReviewEventType.Retracted),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
    }

    private static HistoricalFact CreateRepairableCanonicalFact(Guid factId)
    {
        DateTime recordedAtUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        return new HistoricalFact(
            factId,
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Mirapolis",
                HistoricalSubjectPublicationPolicy.Suppressed),
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForYear(1979)),
            HistoricalFactState.Unverified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            Array.Empty<HistoricalSourceRevisionReference>(),
            null,
            null,
            "history-1",
            null,
            null,
            null,
            1,
            null,
            recordedAtUtc,
            HistoricalRevisionOrigin.Ordinary);
    }

    private static HistoryEvent CloneHistoryEvent(HistoryEvent source)
    {
        string json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<HistoryEvent>(json)
            ?? throw new InvalidOperationException("The history event test fixture could not be cloned.");
    }

    private static Park BuildPark()
    {
        return new Park
        {
            Id = "park-1",
            Name = "Mirapolis",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }

    private async Task<ApplicationResult<ParkGraphUpsertResult>> ProcessAsync(string rawJson, bool apply)
    {
        using JsonDocument document = JsonDocument.Parse(rawJson);
        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            TargetParkId = "park-1",
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = rawJson,
        };

        return apply
            ? await this.Processor.ApplyAsync(request, "user-1", CancellationToken.None)
            : await this.Processor.PreviewAsync(request, "user-1", CancellationToken.None);
    }
}
