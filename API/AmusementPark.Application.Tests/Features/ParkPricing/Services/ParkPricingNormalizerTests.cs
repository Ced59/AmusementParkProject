using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkPricing.Services;

public sealed class ParkPricingNormalizerTests
{
    [Fact]
    public void Normalize_ShouldNormalizeCurrencyCodesAndPreserveSeasonalOffersForSameAudience()
    {
        ParkPricing pricing = new ParkPricing
        {
            ParkId = " park-1 ",
            CurrencyCode = " eur ",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmission("adult-low-season", "adult", 35m, new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30)),
                CreateAdmission("adult-high-season", "adult", 49m, new DateOnly(2026, 7, 1), new DateOnly(2026, 8, 31)),
            },
        };

        ApplicationResult<ParkPricing> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("park-1", result.Value.ParkId);
        Assert.Equal("EUR", result.Value.CurrencyCode);
        Assert.Equal(2, result.Value.AdmissionOffers.Count);
        Assert.All(result.Value.AdmissionOffers, static offer => Assert.Equal("adult", offer.AudienceCategory));
    }

    [Fact]
    public void Normalize_ShouldRejectDuplicateOfferCodes()
    {
        ParkPricing pricing = new ParkPricing
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmission("adult", "adult", 35m, null, null),
                CreateAdmission("ADULT", "child", 20m, null, null),
            },
        };

        ApplicationResult<ParkPricing> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-pricing.invalid");
        Assert.Contains(result.Errors.SelectMany(static error => error.Details ?? new Dictionary<string, IReadOnlyCollection<string>>()),
            static detail => detail.Key == "AdmissionOffers[1].code" && detail.Value.Contains("duplicate"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("12A")]
    public void Normalize_ShouldRejectInvalidCurrencyCode(string currencyCode)
    {
        ParkPricing pricing = new ParkPricing
        {
            ParkId = "park-1",
            CurrencyCode = currencyCode,
        };

        ApplicationResult<ParkPricing> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-pricing.invalid");
    }

    [Fact]
    public void Normalize_ShouldSupportFixedRangeAndDynamicPrices()
    {
        ParkPricing pricing = new ParkPricing
        {
            ParkId = "park-1",
            CurrencyCode = "USD",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                new ParkAdmissionPriceOffer
                {
                    Code = "adult",
                    AudienceCategory = "adult",
                    GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 59m },
                },
                new ParkAdmissionPriceOffer
                {
                    Code = "child-range",
                    AudienceCategory = "child",
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Range, MinimumAmount = 29m, MaximumAmount = 45m },
                },
                new ParkAdmissionPriceOffer
                {
                    Code = "dynamic",
                    AudienceCategory = "adult",
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Dynamic, MinimumAmount = 39m },
                },
            },
        };

        ApplicationResult<ParkPricing> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(ParkPricingMode.Fixed, result.Value.AdmissionOffers[0].GatePrice!.Mode);
        Assert.Equal(ParkPricingMode.Range, result.Value.AdmissionOffers[1].OnlinePrice!.Mode);
        Assert.Equal(ParkPricingMode.Dynamic, result.Value.AdmissionOffers[2].OnlinePrice!.Mode);
    }

    [Fact]
    public void Normalize_ShouldRejectInvertedDateAndPriceRanges()
    {
        ParkPricing pricing = new ParkPricing
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AnnualPasses = new List<ParkAnnualPassOffer>
            {
                new ParkAnnualPassOffer
                {
                    Code = "gold",
                    Names = new List<LocalizedText> { new LocalizedText("fr", "Pass Gold") },
                    OnlinePrice = new ParkPriceValue
                    {
                        Mode = ParkPricingMode.Range,
                        MinimumAmount = 250m,
                        MaximumAmount = 200m,
                    },
                    ValidFrom = new DateOnly(2026, 12, 31),
                    ValidTo = new DateOnly(2026, 1, 1),
                },
            },
        };

        ApplicationResult<ParkPricing> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-pricing.invalid");
    }

    private static ParkAdmissionPriceOffer CreateAdmission(string code, string audience, decimal amount, DateOnly? validFrom, DateOnly? validTo)
    {
        return new ParkAdmissionPriceOffer
        {
            Code = code,
            AudienceCategory = audience,
            GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = amount },
            ValidFrom = validFrom,
            ValidTo = validTo,
        };
    }
}
