using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class ParkPricingMongoMappersTests
{
    [Fact]
    public void ToDocumentToDomain_ShouldPreserveHistoricalCurrencyAndOfferIdentity()
    {
        ParkPricingEntity pricing = new()
        {
            Id = "pricing-1",
            ParkId = "park-1",
            CurrencyCode = "EUR",
            HistoricalSnapshots = new List<ParkPricingSnapshot>
            {
                new ParkPricingSnapshot
                {
                    Id = "snapshot-2024",
                    Year = 2024,
                    CurrencyCode = "HRK",
                    SourceUrl = "https://example.test/prices/2024",
                    LastVerifiedAtUtc = new DateTime(2025, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                    AdmissionOffers = new List<ParkAdmissionPriceOffer>
                    {
                        new ParkAdmissionPriceOffer
                        {
                            Id = "adult-2024",
                            Code = "adult",
                            AudienceCategory = "adult",
                            OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 300m },
                            SortOrder = 1,
                        },
                    },
                },
            },
        };

        ParkPricingEntity result = pricing.ToDocument().ToDomain();

        ParkPricingSnapshot snapshot = Assert.Single(result.HistoricalSnapshots);
        Assert.Equal("snapshot-2024", snapshot.Id);
        Assert.Equal(2024, snapshot.Year);
        Assert.Equal("HRK", snapshot.CurrencyCode);
        Assert.Equal("https://example.test/prices/2024", snapshot.SourceUrl);
        ParkAdmissionPriceOffer offer = Assert.Single(snapshot.AdmissionOffers);
        Assert.Equal("adult-2024", offer.Id);
        Assert.Equal("adult", offer.Code);
        Assert.Equal(300m, offer.OnlinePrice!.Amount);
    }
}
