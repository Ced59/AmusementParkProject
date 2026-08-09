using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Handlers;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Queries;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkPricing.Handlers;

public sealed class ParkPricingQueryHandlersTests
{
    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.TemporarilyClosed)]
    [InlineData(ParkStatus.ClosedDefinitively)]
    [InlineData(ParkStatus.Cancelled)]
    public async Task Query_WhenPublicParkIsNotOperating_ShouldNotExposeStoredPricing(ParkStatus status)
    {
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = status, IsVisible = true });
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        GetParkPricingQueryHandler handler = new(parkRepository.Object, pricingRepository.Object);

        ApplicationResult<ParkPricing> result = await handler.HandleAsync(
            new GetParkPricingQuery("park-1", false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-pricing.not-found");
        pricingRepository.Verify(
            repository => repository.GetByParkIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task Query_WhenPublicPricingIsEmpty_ShouldReturnNotFound()
    {
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = ParkStatus.Operating, IsVisible = true });
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkPricing { ParkId = "park-1", CurrencyCode = "EUR" });
        GetParkPricingQueryHandler handler = new(parkRepository.Object, pricingRepository.Object);

        ApplicationResult<ParkPricing> result = await handler.HandleAsync(
            new GetParkPricingQuery("park-1", false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-pricing.not-found");
        parkRepository.VerifyAll();
        pricingRepository.VerifyAll();
    }

    [Fact]
    public async Task Query_WhenAdminRequestsEmptyPricing_ShouldReturnStoredResource()
    {
        ParkPricing storedPricing = new ParkPricing { ParkId = "park-1", CurrencyCode = "EUR" };
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = ParkStatus.Operating, IsVisible = true });
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedPricing);
        GetParkPricingQueryHandler handler = new(parkRepository.Object, pricingRepository.Object);

        ApplicationResult<ParkPricing> result = await handler.HandleAsync(
            new GetParkPricingQuery("park-1", true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(storedPricing, result.Value);
        parkRepository.VerifyAll();
        pricingRepository.VerifyAll();
    }
}
