using Xunit;

using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class ParkPricingCreditOffersAvailabilityTests
{
    [Fact]
    public void HasPricedOffersValidOn_IncludesCreditOffers()
    {
        ParkPricing pricing = new()
        {
            CreditOffers = new List<ParkCreditOffer>
            {
                new()
                {
                    UnitCode = "token",
                    Quantity = 10,
                    Labels = new List<LocalizedText> { new("fr", "10 jetons") },
                    Prices = new ParkCreditOfferPrices { GatePrice = 2500m },
                    ValidFrom = new DateOnly(2026, 1, 1),
                    ValidTo = new DateOnly(2026, 12, 31),
                },
            },
        };

        Assert.True(pricing.HasPricedOffersValidOn(new DateOnly(2026, 8, 17)));
        Assert.False(pricing.HasPricedOffersValidOn(new DateOnly(2027, 1, 1)));
        Assert.Single(pricing.FilterOffersValidOn(new DateOnly(2026, 8, 17)).CreditOffers);
    }
}
