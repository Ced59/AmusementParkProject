using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class ParkPricingSitemapSectionProviderTests
{
    [Fact]
    public async Task GetUrlsAsync_ShouldOnlyPublishCurrentPricingForOperatingParks()
    {
        Park operating = CreatePark("operating", ParkStatus.Operating);
        Park planned = CreatePark("planned", ParkStatus.Planned);
        Park expired = CreatePark("expired", ParkStatus.Operating);
        IReadOnlyCollection<Park> parks = new[] { operating, planned, expired };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                500,
                false,
                true,
                null,
                null,
                null,
                null,
                ClosedEntityFilter.All,
                It.IsAny<CancellationToken>(),
                ParkAdminSortField.Default,
                false,
                null))
            .ReturnsAsync(new PagedResult<Park>(parks, 1, 500, parks.Count));
        Mock<IParkPricingRepository> pricingRepository = new Mock<IParkPricingRepository>(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.OrderBy(static id => id).SequenceEqual(new[] { "expired", "operating", "planned" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreatePricing("operating", validTo: null),
                CreatePricing("planned", validTo: null),
                CreatePricing("expired", new DateOnly(2020, 12, 31)),
            });
        ParkPricingSitemapSectionProvider provider = new ParkPricingSitemapSectionProvider(
            parkRepository.Object,
            pricingRepository.Object);

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(
            new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } },
            CancellationToken.None);

        SitemapUrlEntry url = Assert.Single(urls);
        Assert.Equal("/fr/park/operating/operating/pricing", url.RelativePath);
        parkRepository.VerifyAll();
        pricingRepository.VerifyAll();
    }

    private static Park CreatePark(string id, ParkStatus status)
    {
        return new Park
        {
            Id = id,
            Name = id,
            IsVisible = true,
            Status = status,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }

    private static ParkPricingEntity CreatePricing(string parkId, DateOnly? validTo)
    {
        return new ParkPricingEntity
        {
            ParkId = parkId,
            CurrencyCode = "EUR",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                new ParkAdmissionPriceOffer
                {
                    Code = "adult",
                    ValidTo = validTo,
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 39m },
                },
            },
        };
    }
}
