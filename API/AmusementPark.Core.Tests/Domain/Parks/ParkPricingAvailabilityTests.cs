using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkPricingAvailabilityTests
{
    [Fact]
    public void FilterOffersValidOn_ShouldKeepOnlyOffersWhoseInclusiveValidityContainsDate()
    {
        DateOnly date = new DateOnly(2026, 8, 9);
        ParkPricing pricing = new ParkPricing
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmissionOffer("current", date, date),
                CreateAdmissionOffer("future", date.AddDays(1), null),
                CreateAdmissionOffer("expired", null, date.AddDays(-1)),
                CreateAdmissionOffer("undated", null, null),
            },
            HistoricalSnapshots = new List<ParkPricingSnapshot>
            {
                new ParkPricingSnapshot { Year = 2024, CurrencyCode = "HRK" },
                new ParkPricingSnapshot { Year = 2025, CurrencyCode = "EUR" },
            },
        };

        ParkPricing filtered = pricing.FilterOffersValidOn(date);

        Assert.Equal(new[] { "current", "undated" }, filtered.AdmissionOffers.Select(static offer => offer.Code));
        Assert.Equal(new[] { 2025, 2024 }, filtered.HistoricalSnapshots.Select(static snapshot => snapshot.Year));
        Assert.Equal(4, pricing.AdmissionOffers.Count);
    }

    [Fact]
    public void HasPricedOffersValidOn_ShouldIgnoreUnpricedAndOutOfPeriodOffers()
    {
        DateOnly date = new DateOnly(2026, 8, 9);
        ParkPricing pricing = new ParkPricing
        {
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmissionOffer("expired", null, date.AddDays(-1)),
                new ParkAdmissionPriceOffer
                {
                    Code = "unpriced",
                    ValidFrom = date,
                    ValidTo = date,
                },
            },
        };

        Assert.False(pricing.HasPricedOffersValidOn(date));

        pricing.ParkingOffers.Add(new ParkParkingPriceOffer
        {
            Code = "current-parking",
            GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 20m },
            ValidFrom = date,
        });

        Assert.True(pricing.HasPricedOffersValidOn(date));
    }

    private static ParkAdmissionPriceOffer CreateAdmissionOffer(string code, DateOnly? validFrom, DateOnly? validTo)
    {
        return new ParkAdmissionPriceOffer
        {
            Code = code,
            OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 39m },
            ValidFrom = validFrom,
            ValidTo = validTo,
        };
    }
}
