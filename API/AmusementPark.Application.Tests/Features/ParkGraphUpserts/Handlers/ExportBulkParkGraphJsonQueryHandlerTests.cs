using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Handlers;

public sealed class ExportBulkParkGraphJsonQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSectionsAreParkOnly_ShouldExportWithoutPerParkGraphExport()
    {
        Park firstPark = new Park
        {
            Id = "park-1",
            Name = "First Park",
            CountryCode = "FR",
            Type = ParkType.ThemePark,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Park secondPark = new Park
        {
            Id = "park-2",
            Name = "Second Park",
            CountryCode = "BE",
            Type = ParkType.WaterPark,
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1", "park-2" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstPark, secondPark });

        ExportBulkParkGraphJsonQueryHandler handler = CreateHandler(
            parkRepository.Object);

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportBulkParkGraphJsonQuery(new ParkGraphBulkExportRequest
            {
                SelectionMode = ParkGraphBulkParkSelectionMode.Explicit,
                ParkIds = new[] { "park-1", "park-2" },
                Sections = new[] { ParkGraphExportSection.ParkBasics },
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        using JsonDocument document = JsonDocument.Parse(result.Value.Content);
        JsonElement root = document.RootElement;
        JsonElement parks = root.GetProperty("parks");

        Assert.Equal(2, parks.GetArrayLength());
        Assert.Equal("park-1", parks[0].GetProperty("identity").GetProperty("parkId").GetString());
        Assert.Equal("First Park", parks[0].GetProperty("park").GetProperty("name").GetString());
        Assert.Equal("ThemePark", parks[0].GetProperty("park").GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Null, parks[0].GetProperty("park").GetProperty("websiteUrl").ValueKind);
        Assert.Equal("park-2", parks[1].GetProperty("identity").GetProperty("parkId").GetString());
        Assert.Equal(2, root.GetProperty("selection").GetProperty("exportedParkCount").GetInt32());
        Assert.Equal("ParkBasics", root.GetProperty("selection").GetProperty("sections")[0].GetString());

        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSectionsNeedGraphExport_ShouldUseBulkRepositoriesWithoutUnitParkQuery()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Graph Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

        ExportBulkParkGraphJsonQueryHandler handler = CreateHandler(
            parkRepository.Object,
            parkItemRepository: parkItemRepository.Object);

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportBulkParkGraphJsonQuery(new ParkGraphBulkExportRequest
            {
                SelectionMode = ParkGraphBulkParkSelectionMode.Explicit,
                ParkIds = new[] { "park-1" },
                Sections = new[] { ParkGraphExportSection.Items },
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        using JsonDocument document = JsonDocument.Parse(result.Value.Content);

        Assert.Equal("park-1", document.RootElement.GetProperty("parks")[0].GetProperty("identity").GetProperty("parkId").GetString());
        Assert.True(document.RootElement.GetProperty("parks")[0].TryGetProperty("items", out JsonElement items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);

        parkRepository.VerifyAll();
        parkRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        parkItemRepository.VerifyAll();
        parkItemRepository.Verify(repository => repository.GetByParkIdAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenOnlyImagesAreSelected_ShouldNotLoadItemOwners()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Images Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Image parkImage = new Image
        {
            Id = "image-park-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            IsPublished = true,
            OriginalFileName = "park.jpg",
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.Park,
                It.Is<IReadOnlyCollection<string>>(ownerIds => ownerIds.SequenceEqual(new[] { "park-1" })),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkImage });

        ExportBulkParkGraphJsonQueryHandler handler = CreateHandler(
            parkRepository.Object,
            parkItemRepository: parkItemRepository.Object,
            imageRepository: imageRepository.Object);

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportBulkParkGraphJsonQuery(new ParkGraphBulkExportRequest
            {
                SelectionMode = ParkGraphBulkParkSelectionMode.Explicit,
                ParkIds = new[] { "park-1" },
                Sections = new[] { ParkGraphExportSection.Images },
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        using JsonDocument document = JsonDocument.Parse(result.Value.Content);
        JsonElement images = document.RootElement.GetProperty("parks")[0].GetProperty("images");

        Assert.Equal(1, images.GetArrayLength());
        Assert.Equal("image-park-1", images[0].GetProperty("imageId").GetString());
        Assert.Equal("park", images[0].GetProperty("ownerKey").GetString());
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
        parkItemRepository.Verify(repository => repository.GetByParkIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenHistoryIsSelected_ShouldPreserveHistoryIdentifiers()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "History Park",
            CountryCode = "FR",
        };
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "History Item",
        };
        HistoryEvent historyEvent = new HistoryEvent
        {
            Key = "item-opening",
            EntityType = HistoryEntityType.ParkItem,
            OwnerId = "item-1",
            ParkItemId = "item-1",
            ContextParkId = "park-1",
            Year = 2001,
            EventType = ParkItemHistoryEventType.Opening.ToString(),
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        Mock<IHistoryEventRepository> historyEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyEventRepository
            .Setup(repository => repository.GetParkTimelineAsync(
                "park-1",
                true,
                true,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { historyEvent });

        ExportBulkParkGraphJsonQueryHandler handler = CreateHandler(
            parkRepository.Object,
            parkItemRepository: parkItemRepository.Object,
            historyEventRepository: historyEventRepository.Object);

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportBulkParkGraphJsonQuery(new ParkGraphBulkExportRequest
            {
                SelectionMode = ParkGraphBulkParkSelectionMode.Explicit,
                ParkIds = new[] { "park-1" },
                Sections = new[] { ParkGraphExportSection.History },
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        using JsonDocument document = JsonDocument.Parse(result.Value.Content);
        JsonElement exportedEvent = document.RootElement
            .GetProperty("parks")[0]
            .GetProperty("history")
            .GetProperty("events")[0];

        Assert.Equal("item-1", exportedEvent.GetProperty("ownerId").GetString());
        Assert.Equal("item-1", exportedEvent.GetProperty("parkItemId").GetString());
        Assert.Equal("park-1", exportedEvent.GetProperty("contextParkId").GetString());
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        historyEventRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenProgressIsProvided_ShouldReportCompletedProgress()
    {
        Park firstPark = new Park
        {
            Id = "park-1",
            Name = "First Park",
            CountryCode = "FR",
        };
        Park secondPark = new Park
        {
            Id = "park-2",
            Name = "Second Park",
            CountryCode = "BE",
        };
        List<ParkGraphJsonExportProgress> progressReports = new List<ParkGraphJsonExportProgress>();

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1", "park-2" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstPark, secondPark });

        ExportBulkParkGraphJsonQueryHandler handler = CreateHandler(
            parkRepository.Object);

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportBulkParkGraphJsonQuery(
                new ParkGraphBulkExportRequest
                {
                    SelectionMode = ParkGraphBulkParkSelectionMode.Explicit,
                    ParkIds = new[] { "park-1", "park-2" },
                    Sections = Array.Empty<ParkGraphExportSection>(),
                },
                new CollectingProgress<ParkGraphJsonExportProgress>(progressReports)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(progressReports, report => report.ProgressPercentage == 100 && report.ProcessedParkCount == 2 && report.ExportedParkCount == 2);
        Assert.Contains(progressReports, report => report.Step == "selection" && report.ExportedParkCount == 2);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenOutputStreamIsProvided_ShouldWriteJsonToStreamWithoutBufferingContent()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Stream Park",
            CountryCode = "FR",
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });

        ExportBulkParkGraphJsonQueryHandler handler = CreateHandler(
            parkRepository.Object);
        await using MemoryStream output = new MemoryStream();

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportBulkParkGraphJsonQuery(
                new ParkGraphBulkExportRequest
                {
                    SelectionMode = ParkGraphBulkParkSelectionMode.Explicit,
                    ParkIds = new[] { "park-1" },
                    Sections = Array.Empty<ParkGraphExportSection>(),
                },
                null,
                output),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Content);

        output.Position = 0;
        using JsonDocument document = await JsonDocument.ParseAsync(output);

        Assert.Equal("park-1", document.RootElement.GetProperty("parks")[0].GetProperty("identity").GetProperty("parkId").GetString());
        parkRepository.VerifyAll();
    }

    private static ExportBulkParkGraphJsonQueryHandler CreateHandler(
        IParkRepository parkRepository,
        IParkItemRepository? parkItemRepository = null,
        IImageRepository? imageRepository = null,
        IHistoryEventRepository? historyEventRepository = null)
    {
        return new ExportBulkParkGraphJsonQueryHandler(
            parkRepository,
            Mock.Of<IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>>>(MockBehavior.Strict),
            new BulkParkGraphJsonExportDataLoader(
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository ?? Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository ?? Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IParkOpeningHoursRepository>(MockBehavior.Strict),
            historyEventRepository ?? Mock.Of<IHistoryEventRepository>(MockBehavior.Strict)));
    }

    private sealed class CollectingProgress<TProgress> : IProgress<TProgress>
    {
        private readonly ICollection<TProgress> reports;

        public CollectingProgress(ICollection<TProgress> reports)
        {
            this.reports = reports;
        }

        public void Report(TProgress value)
        {
            this.reports.Add(value);
        }
    }
}
