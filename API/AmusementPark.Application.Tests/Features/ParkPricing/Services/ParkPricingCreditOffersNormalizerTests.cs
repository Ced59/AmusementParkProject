using Xunit;

using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Tests.Features.ParkPricing.Services;

public sealed class ParkPricingCreditOffersNormalizerTests
{
    [Fact]
    public void Normalize_AcceptsCreditBundleAndNormalizesUnitCode()
    {
        ParkPricingEntity pricing = CreatePricing();
        pricing.CreditOffers.Add(new ParkCreditOffer
        {
            UnitCode = " Token ",
            Quantity = 10,
            Labels = Localized("10 tokens"),
            Prices = new ParkCreditOfferPrices { GatePrice = 2500m },
            SortOrder = 1,
        });

        var result = ParkPricingNormalizer.Normalize(pricing);

        Assert.True(result.IsSuccess);
        Assert.Equal("token", result.Value!.CreditOffers.Single().UnitCode);
        Assert.Equal(2500m, result.Value.CreditOffers.Single().Prices.GatePrice);
    }

    [Fact]
    public void Normalize_RejectsDuplicateUnitAndQuantity()
    {
        ParkPricingEntity pricing = CreatePricing();
        pricing.CreditOffers.Add(CreateOffer(10, 2500m));
        pricing.CreditOffers.Add(CreateOffer(10, 2400m));

        var result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
    }

    private static ParkPricingEntity CreatePricing() => new()
    {
        ParkId = "park-1",
        CurrencyCode = "RSD",
        AdmissionOffers = new List<ParkAdmissionPriceOffer>
        {
            new()
            {
                Code = "entry",
                AudienceCategory = "general",
                Labels = Localized("Entry"),
                GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 1m },
                SortOrder = 1,
            },
        },
    };

    private static ParkCreditOffer CreateOffer(int quantity, decimal amount) => new()
    {
        UnitCode = "token",
        Quantity = quantity,
        Labels = Localized($"{quantity} tokens"),
        Prices = new ParkCreditOfferPrices { GatePrice = amount },
        SortOrder = 1,
    };

    private static List<LocalizedText> Localized(string value) => new()
    {
        new("fr", value), new("en", value), new("es", value), new("de", value),
        new("it", value), new("nl", value), new("pt", value), new("pl", value),
    };
}
