using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
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
        this.HistoryEventRepository
            .Setup(value => value.GetByOwnerKeyAsync(
                HistoryEntityType.Park,
                "park-1",
                "history-1979",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CloneHistoryEvent(this.persistedEvent));
        this.HistoryEventRepository
            .Setup(value => value.UpdateAsync("history-1", It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()))
            .Callback<string, HistoryEvent, CancellationToken>((_, historyEvent, _) =>
            {
                this.persistedEvent = CloneHistoryEvent(historyEvent);
            })
            .ReturnsAsync((string _, HistoryEvent historyEvent, CancellationToken _) => CloneHistoryEvent(historyEvent));

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
            historyEventRepository: this.HistoryEventRepository.Object);
    }

    public Mock<IHistoryEventRepository> HistoryEventRepository { get; }

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
