using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Commands;
using AmusementPark.Application.Features.ParkPricing.Handlers;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Tests.Features.ParkPricing.Handlers;

public sealed class ParkPricingCommandHandlersTests
{
    [Fact]
    public async Task HandleAsync_WhenLegacyClientOmitsHistory_ShouldPreserveStoredSnapshots()
    {
        ParkPricingEntity pricing = CreateValidPricing();
        ParkPricingEntity existingPricing = CreateValidPricing();
        existingPricing.HistoricalSnapshots = new List<ParkPricingSnapshot>
        {
            new ParkPricingSnapshot
            {
                Year = 2025,
                CurrencyCode = "EUR",
                AdmissionOffers = CreateAdmissionOffers(35m),
            },
        };
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = ParkStatus.Operating });
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPricing);
        pricingRepository
            .Setup(repository => repository.UpsertAsync(It.IsAny<ParkPricingEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkPricingEntity value, CancellationToken _) => value);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler = new(MockBehavior.Strict);
        sitemapRefreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        UpsertParkPricingCommandHandler handler = new(
            parkRepository.Object,
            pricingRepository.Object,
            sitemapRefreshScheduler.Object);

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
            new UpsertParkPricingCommand(pricing, PreserveHistoricalSnapshots: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        ParkPricingSnapshot snapshot = Assert.Single(result.Value!.HistoricalSnapshots);
        Assert.Equal(2025, snapshot.Year);
        pricingRepository.VerifyAll();
        parkRepository.VerifyAll();
        sitemapRefreshScheduler.VerifyAll();
    }

    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.TemporarilyClosed)]
    [InlineData(ParkStatus.ClosedDefinitively)]
    [InlineData(ParkStatus.Cancelled)]
    public async Task HandleAsync_WhenParkIsNotOperating_ShouldRejectCurrentPricing(ParkStatus status)
    {
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                new ParkAdmissionPriceOffer
                {
                    Code = "adult",
                    AudienceCategory = "adult",
                    Labels = new[] { "fr", "en", "es", "de", "it", "nl", "pt", "pl" }
                        .Select(static languageCode => new LocalizedText(languageCode, "Adult"))
                        .ToList(),
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 39m },
                },
            },
        };
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Status = status });
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler = new(MockBehavior.Strict);
        UpsertParkPricingCommandHandler handler = new(
            parkRepository.Object,
            pricingRepository.Object,
            sitemapRefreshScheduler.Object);

        ApplicationResult<ParkPricingEntity> result = await handler.HandleAsync(
            new UpsertParkPricingCommand(pricing),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-pricing.not-operating");
        pricingRepository.Verify(
            repository => repository.UpsertAsync(It.IsAny<ParkPricingEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        sitemapRefreshScheduler.Verify(
            scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        parkRepository.VerifyAll();
    }

    private static ParkPricingEntity CreateValidPricing()
    {
        return new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AdmissionOffers = CreateAdmissionOffers(39m),
        };
    }

    private static List<ParkAdmissionPriceOffer> CreateAdmissionOffers(decimal amount)
    {
        return new List<ParkAdmissionPriceOffer>
        {
            new ParkAdmissionPriceOffer
            {
                Code = "adult",
                AudienceCategory = "adult",
                Labels = new[] { "fr", "en", "es", "de", "it", "nl", "pt", "pl" }
                    .Select(static languageCode => new LocalizedText(languageCode, "Adult"))
                    .ToList(),
                OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = amount },
            },
        };
    }
}
