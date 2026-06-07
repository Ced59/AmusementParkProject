using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Handlers;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems.Handlers;

public sealed class ParkItemsBulkCreateCommandHandlerTests
{
    [Fact]
    public async Task PreviewAsync_WhenReferencesResolveByName_ShouldReturnApplicableWarningRow()
    {
        Fixture fixture = new Fixture();
        fixture.SetupPark();
        fixture.SetupReferenceData();
        fixture.ParkItemRepository
            .Setup(item => item.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ParkItem { Id = "existing-1", ParkId = "park-1", Name = "Blue Fire" } });

        PreviewParkItemsBulkCreateCommandHandler handler = new PreviewParkItemsBulkCreateCommandHandler(fixture.CreatePreviewService());

        ApplicationResult<ParkItemsBulkCreatePreviewResult> result = await handler.HandleAsync(
            new PreviewParkItemsBulkCreateCommand(
                " park-1 ",
                new[]
                {
                    new ParkItemBulkCreateDraft
                    {
                        RowNumber = 1,
                        Name = "Arthur",
                        Category = ParkItemCategory.Attraction,
                        Type = ParkItemType.RollerCoaster,
                        ZoneName = "Iceland",
                        ManufacturerName = "Mack Rides",
                    },
                }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        ParkItemBulkCreatePreviewRow row = Assert.Single(result.Value.Rows);
        Assert.True(row.CanApply);
        Assert.Equal("zone-1", row.ZoneId);
        Assert.Equal("manufacturer-1", row.ManufacturerId);
        Assert.Contains("zone.resolved-by-name", row.Warnings);
        Assert.Contains("manufacturer.resolved-by-name", row.Warnings);
        Assert.Equal(0, result.Value.ErrorCount);
        fixture.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WhenZoneIsUnknown_ShouldReturnBlockingRowError()
    {
        Fixture fixture = new Fixture();
        fixture.SetupPark();
        fixture.SetupReferenceData();
        fixture.ParkItemRepository
            .Setup(item => item.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

        PreviewParkItemsBulkCreateCommandHandler handler = new PreviewParkItemsBulkCreateCommandHandler(fixture.CreatePreviewService());

        ApplicationResult<ParkItemsBulkCreatePreviewResult> result = await handler.HandleAsync(
            new PreviewParkItemsBulkCreateCommand(
                "park-1",
                new[]
                {
                    new ParkItemBulkCreateDraft
                    {
                        RowNumber = 1,
                        Name = "Unknown Zone Ride",
                        ZoneName = "Missing Zone",
                    },
                }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        ParkItemBulkCreatePreviewRow row = Assert.Single(result.Value.Rows);
        Assert.False(row.CanApply);
        Assert.Contains("zone.unknown", row.Errors);
        Assert.Equal(1, result.Value.ErrorCount);
        fixture.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_ShouldCreateApplicableRowsOnlyAndRefreshSearch()
    {
        Fixture fixture = new Fixture();
        fixture.SetupPark();
        fixture.SetupReferenceData();
        fixture.ParkItemRepository
            .Setup(item => item.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        fixture.ParkItemRepository
            .Setup(item => item.CreateAsync(It.Is<ParkItem>(parkItem =>
                parkItem.ParkId == "park-1" &&
                parkItem.Name == "Arthur" &&
                parkItem.IsVisible == false &&
                parkItem.AdminReviewStatus == AdminReviewStatus.ToReview), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkItem { Id = "created-1", ParkId = "park-1", Name = "Arthur" });
        fixture.SearchProjectionWriter
            .Setup(item => item.UpsertManyAsync(
                SearchProjectionResourceTypes.ParkItems,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "created-1" })),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        fixture.SearchProjectionWriter
            .Setup(item => item.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ApplyParkItemsBulkCreateCommandHandler handler = new ApplyParkItemsBulkCreateCommandHandler(
            fixture.CreatePreviewService(),
            fixture.ParkItemRepository.Object,
            fixture.SearchProjectionWriter.Object);

        ApplicationResult<ParkItemsBulkCreateApplyResult> result = await handler.HandleAsync(
            new ApplyParkItemsBulkCreateCommand(
                "park-1",
                new[]
                {
                    new ParkItemBulkCreateDraft { RowNumber = 1, Name = "Arthur" },
                    new ParkItemBulkCreateDraft { RowNumber = 2, Name = "Broken", ZoneName = "Missing Zone" },
                }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.RequestedCount);
        Assert.Equal(1, result.Value.CreatedCount);
        Assert.Equal(1, result.Value.IgnoredCount);
        Assert.Equal(new[] { "created-1" }, result.Value.CreatedIds);
        fixture.VerifyAll();
    }

    private sealed class Fixture
    {
        public Mock<IParkRepository> ParkRepository { get; } = new Mock<IParkRepository>(MockBehavior.Strict);

        public Mock<IParkZoneRepository> ParkZoneRepository { get; } = new Mock<IParkZoneRepository>(MockBehavior.Strict);

        public Mock<IAttractionManufacturerRepository> AttractionManufacturerRepository { get; } = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);

        public Mock<IParkItemRepository> ParkItemRepository { get; } = new Mock<IParkItemRepository>(MockBehavior.Strict);

        public Mock<ISearchProjectionWriter> SearchProjectionWriter { get; } = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);

        public void SetupPark()
        {
            this.ParkRepository
                .Setup(item => item.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
        }

        public void SetupReferenceData()
        {
            this.ParkZoneRepository
                .Setup(item => item.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new ParkZone
                    {
                        Id = "zone-1",
                        ParkId = "park-1",
                        Name = "Iceland",
                        Names = new List<LocalizedText> { new LocalizedText("fr", "Islande") },
                    },
                });
            this.AttractionManufacturerRepository
                .Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new AttractionManufacturer
                    {
                        Id = "manufacturer-1",
                        Name = "Mack Rides",
                    },
                });
        }

        public ParkItemsBulkCreatePreviewService CreatePreviewService()
        {
            return new ParkItemsBulkCreatePreviewService(
                this.ParkItemRepository.Object,
                this.ParkZoneRepository.Object,
                this.AttractionManufacturerRepository.Object,
                new ParkItemReferenceValidator(
                    this.ParkRepository.Object,
                    this.ParkZoneRepository.Object,
                    this.AttractionManufacturerRepository.Object));
        }

        public void VerifyAll()
        {
            this.ParkRepository.VerifyAll();
            this.ParkZoneRepository.VerifyAll();
            this.AttractionManufacturerRepository.VerifyAll();
            this.ParkItemRepository.VerifyAll();
            this.SearchProjectionWriter.VerifyAll();
        }
    }
}
