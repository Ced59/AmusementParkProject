using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Handlers;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Tests.Features.StandaloneAttractions.Handlers;

public sealed class GetStandaloneAttractionDataCompletenessScoreQueryHandlerTests
{
    [Theory]
    [InlineData(" ")]
    [InlineData("missing")]
    public async Task MissingAttraction_ReturnsFailureWithoutLoadingRelatedData(string id)
    {
        Mock<IStandaloneAttractionRepository> attractions = new(MockBehavior.Strict);
        if (!string.IsNullOrWhiteSpace(id))
        {
            attractions.Setup(repository => repository.GetByIdAsync(id, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StandaloneAttraction?)null);
        }
        GetStandaloneAttractionDataCompletenessScoreQueryHandler handler = new(attractions.Object,
            Mock.Of<IImageRepository>(MockBehavior.Strict), Mock.Of<IHistoryEventRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict), Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IStandaloneAttractionOpeningHoursRepository>(MockBehavior.Strict), Mock.Of<IStandaloneAttractionPricingRepository>(MockBehavior.Strict));

        var result = await handler.HandleAsync(new(id));

        Assert.False(result.IsSuccess);
        attractions.VerifyAll();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExactNativeTarget_LoadsLinkedFactsAndHonorsProjectionAndOperatingStatus(bool projected, bool operating)
    {
        StandaloneAttraction attraction = new()
        {
            Id = "standalone-1", Name = "Cliff Flight", OperatorId = "operator-1", LegacyParkId = "legacy-park",
            AttractionDetails = new() { Status = operating ? "Operating" : "ClosedDefinitively", ManufacturerId = "manufacturer-1" },
        };
        Mock<IStandaloneAttractionRepository> attractions = new(MockBehavior.Strict);
        attractions.Setup(repository => repository.GetByIdAsync("standalone-1", true, It.IsAny<CancellationToken>())).ReturnsAsync(attraction);
        Mock<IImageRepository> images = new(MockBehavior.Strict);
        Image candidate = new() { Id = "image-1", OwnerId = "standalone-1", OwnerType = ImageOwnerType.StandaloneAttraction,
            IsPublished = false, IsCurrent = true, Captions = new() { new("fr", "Cette page publique présente l'inventaire actuel.") } };
        images.Setup(repository => repository.GetByOwnersAsync(ImageOwnerType.StandaloneAttraction,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "standalone-1" })), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { candidate });
        images.Setup(repository => repository.GetByOwnersAsync(ImageOwnerType.ParkOperator,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "operator-1" })), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        images.Setup(repository => repository.GetByOwnersAsync(ImageOwnerType.AttractionManufacturer,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "manufacturer-1" })), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        Mock<IHistoryEventRepository> history = new(MockBehavior.Strict);
        history.Setup(repository => repository.GetOwnerTimelineAsync(HistoryEntityType.StandaloneAttraction,
            "standalone-1", true, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<HistoryEvent>());
        Mock<IParkOperatorRepository> operators = new(MockBehavior.Strict);
        operators.Setup(repository => repository.GetByIdAsync("operator-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkOperator { Id = "operator-1" });
        Mock<IAttractionManufacturerRepository> manufacturers = new(MockBehavior.Strict);
        manufacturers.Setup(repository => repository.GetByIdAsync("manufacturer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AttractionManufacturer { Id = "manufacturer-1" });
        Mock<IStandaloneAttractionOpeningHoursRepository> hours = new(MockBehavior.Strict);
        Mock<IStandaloneAttractionPricingRepository> prices = new(MockBehavior.Strict);
        if (operating)
        {
            hours.Setup(repository => repository.GetByStandaloneAttractionIdAsync("standalone-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ParkOpeningHoursSchedule?)null);
            prices.Setup(repository => repository.GetByStandaloneAttractionIdAsync("standalone-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ParkPricingEntity?)null);
        }
        GetStandaloneAttractionDataCompletenessScoreQueryHandler handler = new(attractions.Object, images.Object, history.Object,
            operators.Object, manufacturers.Object, hours.Object, prices.Object);

        var result = await handler.HandleAsync(new(" standalone-1 ", ProjectForPublication: projected));

        Assert.True(result.IsSuccess);
        Assert.Equal(projected ? DataCompletenessScoringRules.ForbiddenPublicTextPublicationBlocker : null, result.Value!.PublicationBlocker);
        Assert.False(candidate.IsPublished);
        Assert.False(attraction.IsVisible);
        attractions.VerifyAll(); images.VerifyAll(); history.VerifyAll(); operators.VerifyAll(); manufacturers.VerifyAll();
        hours.VerifyAll(); prices.VerifyAll();
    }
}
