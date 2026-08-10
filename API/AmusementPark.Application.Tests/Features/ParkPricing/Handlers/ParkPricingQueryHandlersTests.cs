using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Handlers;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Queries;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

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

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
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
            .ReturnsAsync(new ParkPricingEntity { ParkId = "park-1", CurrencyCode = "EUR" });
        GetParkPricingQueryHandler handler = new(parkRepository.Object, pricingRepository.Object);

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
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
        ParkPricingEntity storedPricing = new ParkPricingEntity { ParkId = "park-1", CurrencyCode = "EUR" };
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = ParkStatus.Operating, IsVisible = true });
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedPricing);
        GetParkPricingQueryHandler handler = new(parkRepository.Object, pricingRepository.Object);

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
            new GetParkPricingQuery("park-1", true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(storedPricing, result.Value);
        parkRepository.VerifyAll();
        pricingRepository.VerifyAll();
    }

    [Fact]
    public async Task Query_WhenPublicOperatingParkHasPricing_ShouldReturnStoredResource()
    {
        ParkPricingEntity storedPricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                new ParkAdmissionPriceOffer
                {
                    Code = "adult",
                    AudienceCategory = "adult",
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 39m },
                },
            },
        };
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = ParkStatus.Operating, IsVisible = true });
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedPricing);
        GetParkPricingQueryHandler handler = new(parkRepository.Object, pricingRepository.Object);

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
            new GetParkPricingQuery("park-1", false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotSame(storedPricing, result.Value);
        Assert.Single(result.Value!.AdmissionOffers);
        parkRepository.VerifyAll();
        pricingRepository.VerifyAll();
    }

    [Fact]
    public async Task Query_WhenPublicPricingContainsSeasonalOffers_ShouldReturnOnlyOffersValidToday()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);
        DateOnly today = DateOnly.FromDateTime(now.UtcDateTime);
        ParkPricingEntity storedPricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmissionOffer("current", today, today),
                CreateAdmissionOffer("future", today.AddDays(1), null),
            },
            AnnualPasses = new List<ParkAnnualPassOffer>
            {
                new ParkAnnualPassOffer
                {
                    Code = "expired-pass",
                    GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 99m },
                    ValidTo = today.AddDays(-1),
                },
            },
        };
        Mock<IParkRepository> parkRepository = CreateOperatingParkRepository(false);
        Mock<IParkPricingRepository> pricingRepository = CreatePricingRepository(storedPricing);
        GetParkPricingQueryHandler handler = new GetParkPricingQueryHandler(
            parkRepository.Object,
            pricingRepository.Object,
            new FixedTimeProvider(now));

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
            new GetParkPricingQuery("park-1", false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        ParkAdmissionPriceOffer offer = Assert.Single(result.Value!.AdmissionOffers);
        Assert.Equal("current", offer.Code);
        Assert.Empty(result.Value.AnnualPasses);
        parkRepository.VerifyAll();
        pricingRepository.VerifyAll();
    }

    [Fact]
    public async Task Query_WhenAllPublicOffersAreOutsideTheirValidityPeriod_ShouldReturnNotFound()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);
        DateOnly today = DateOnly.FromDateTime(now.UtcDateTime);
        ParkPricingEntity storedPricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmissionOffer("expired", null, today.AddDays(-1)),
                CreateAdmissionOffer("future", today.AddDays(1), null),
            },
        };
        Mock<IParkRepository> parkRepository = CreateOperatingParkRepository(false);
        Mock<IParkPricingRepository> pricingRepository = CreatePricingRepository(storedPricing);
        GetParkPricingQueryHandler handler = new GetParkPricingQueryHandler(
            parkRepository.Object,
            pricingRepository.Object,
            new FixedTimeProvider(now));

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
            new GetParkPricingQuery("park-1", false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-pricing.not-found");
        parkRepository.VerifyAll();
        pricingRepository.VerifyAll();
    }

    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.ClosedDefinitively)]
    public async Task Query_WhenAdminRequestsNonOperatingPark_ShouldReturnStoredResource(ParkStatus status)
    {
        ParkPricingEntity storedPricing = new ParkPricingEntity { ParkId = "park-1", CurrencyCode = "EUR" };
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = status, IsVisible = false });
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedPricing);
        GetParkPricingQueryHandler handler = new(parkRepository.Object, pricingRepository.Object);

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
            new GetParkPricingQuery("park-1", true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(storedPricing, result.Value);
        parkRepository.VerifyAll();
        pricingRepository.VerifyAll();
    }

    private static ParkAdmissionPriceOffer CreateAdmissionOffer(string code, DateOnly? validFrom, DateOnly? validTo)
    {
        return new ParkAdmissionPriceOffer
        {
            Code = code,
            AudienceCategory = "adult",
            OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 39m },
            ValidFrom = validFrom,
            ValidTo = validTo,
        };
    }

    private static Mock<IParkRepository> CreateOperatingParkRepository(bool includeHidden)
    {
        Mock<IParkRepository> repository = new Mock<IParkRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByIdAsync("park-1", includeHidden, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = ParkStatus.Operating, IsVisible = true });
        return repository;
    }

    private static Mock<IParkPricingRepository> CreatePricingRepository(ParkPricingEntity pricing)
    {
        Mock<IParkPricingRepository> repository = new Mock<IParkPricingRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);
        return repository;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            this.now = now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return this.now;
        }
    }
}
