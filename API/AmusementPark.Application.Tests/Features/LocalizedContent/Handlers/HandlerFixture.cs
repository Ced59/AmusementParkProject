using AmusementPark.Application.Errors;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.LocalizedContent.Commands;
using AmusementPark.Application.Features.LocalizedContent.Handlers;
using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LocalizedContent.Handlers;

internal sealed class HandlerFixture
{
    public Mock<IParkRepository> ParkRepository { get; } = new Mock<IParkRepository>(MockBehavior.Strict);
    public Mock<IParkZoneRepository> ParkZoneRepository { get; } = new Mock<IParkZoneRepository>(MockBehavior.Strict);
    public Mock<IParkItemRepository> ParkItemRepository { get; } = new Mock<IParkItemRepository>(MockBehavior.Strict);
    public Mock<IParkOperatorRepository> ParkOperatorRepository { get; } = new Mock<IParkOperatorRepository>(MockBehavior.Strict);
    public Mock<IParkFounderRepository> ParkFounderRepository { get; } = new Mock<IParkFounderRepository>(MockBehavior.Strict);
    public Mock<IAttractionManufacturerRepository> AttractionManufacturerRepository { get; } = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
    public Mock<IImageRepository> ImageRepository { get; } = new Mock<IImageRepository>(MockBehavior.Strict);
    public Mock<IImageTagRepository> ImageTagRepository { get; } = new Mock<IImageTagRepository>(MockBehavior.Strict);
    public Mock<IAttractionAccessConditionTypeDefinitionRepository> AccessConditionTypeDefinitionRepository { get; } = new Mock<IAttractionAccessConditionTypeDefinitionRepository>(MockBehavior.Strict);
    public Mock<ISearchProjectionWriter> SearchProjectionWriter { get; } = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);

    public ApplyLocalizedContentJsonCommandHandler CreateHandler()
    {
        return new ApplyLocalizedContentJsonCommandHandler(
            ParkRepository.Object,
            ParkZoneRepository.Object,
            ParkItemRepository.Object,
            ParkOperatorRepository.Object,
            ParkFounderRepository.Object,
            AttractionManufacturerRepository.Object,
            ImageRepository.Object,
            ImageTagRepository.Object,
            AccessConditionTypeDefinitionRepository.Object,
            SearchProjectionWriter.Object,
            MeasurementConversionService.Instance);
    }

    public void VerifyAll()
    {
        ParkRepository.VerifyAll();
        ParkItemRepository.VerifyAll();
        SearchProjectionWriter.VerifyAll();
    }

    public void VerifyNoOtherCalls()
    {
        ParkRepository.VerifyNoOtherCalls();
        ParkZoneRepository.VerifyNoOtherCalls();
        ParkItemRepository.VerifyNoOtherCalls();
        ParkOperatorRepository.VerifyNoOtherCalls();
        ParkFounderRepository.VerifyNoOtherCalls();
        AttractionManufacturerRepository.VerifyNoOtherCalls();
        ImageRepository.VerifyNoOtherCalls();
        ImageTagRepository.VerifyNoOtherCalls();
        AccessConditionTypeDefinitionRepository.VerifyNoOtherCalls();
        SearchProjectionWriter.VerifyNoOtherCalls();
    }
}
