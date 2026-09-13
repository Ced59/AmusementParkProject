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
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems.Handlers;

internal sealed class Fixture
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
