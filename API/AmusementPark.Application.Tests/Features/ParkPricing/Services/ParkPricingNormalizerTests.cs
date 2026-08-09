using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Xunit;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Tests.Features.ParkPricing.Services;

public sealed class ParkPricingNormalizerTests
{
    [Fact]
    public void Normalize_ShouldNormalizeCurrencyCodesAndPreserveSeasonalOffersForSameAudience()
    {
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = " park-1 ",
            CurrencyCode = " eur ",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmission("adult-low-season", "adult", 35m, new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30)),
                CreateAdmission("adult-high-season", "adult", 49m, new DateOnly(2026, 7, 1), new DateOnly(2026, 8, 31)),
            },
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

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
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmission("adult", "adult", 35m, null, null),
                CreateAdmission("ADULT", "child", 20m, null, null),
            },
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

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
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = currencyCode,
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-pricing.invalid");
    }

    [Fact]
    public void Normalize_ShouldSupportFixedRangeAndDynamicPrices()
    {
        ParkPricingEntity pricing = new ParkPricingEntity
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

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(ParkPricingMode.Fixed, result.Value.AdmissionOffers[0].GatePrice!.Mode);
        Assert.Equal(ParkPricingMode.Range, result.Value.AdmissionOffers[1].OnlinePrice!.Mode);
        Assert.Equal(ParkPricingMode.Dynamic, result.Value.AdmissionOffers[2].OnlinePrice!.Mode);
    }

    [Fact]
    public void Normalize_ShouldRejectInvertedDateAndPriceRanges()
    {
        ParkPricingEntity pricing = new ParkPricingEntity
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

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park-pricing.invalid");
    }

    [Theory]
    [InlineData(ParkPricingMode.Fixed, -1d, null, null)]
    [InlineData(ParkPricingMode.Range, null, -1d, 10d)]
    [InlineData(ParkPricingMode.Dynamic, null, 10d, -1d)]
    public void Normalize_ShouldRejectNegativePrices(
        ParkPricingMode mode,
        double? amount,
        double? minimumAmount,
        double? maximumAmount)
    {
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            ParkingOffers = new List<ParkParkingPriceOffer>
            {
                new ParkParkingPriceOffer
                {
                    Code = "parking",
                    OnlinePrice = new ParkPriceValue
                    {
                        Mode = mode,
                        Amount = amount.HasValue ? Convert.ToDecimal(amount.Value) : null,
                        MinimumAmount = minimumAmount.HasValue ? Convert.ToDecimal(minimumAmount.Value) : null,
                        MaximumAmount = maximumAmount.HasValue ? Convert.ToDecimal(maximumAmount.Value) : null,
                    },
                },
            },
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors.SelectMany(static error => error.Details ?? new Dictionary<string, IReadOnlyCollection<string>>()),
            static detail => detail.Value.Contains("negative-price"));
    }

    [Fact]
    public void Normalize_ShouldAcceptOnlineOnlyAndGateOnlyOffers()
    {
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                new ParkAdmissionPriceOffer
                {
                    Code = "online-only",
                    AudienceCategory = "adult",
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 39m },
                },
                new ParkAdmissionPriceOffer
                {
                    Code = "gate-only",
                    AudienceCategory = "child",
                    GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 29m },
                },
            },
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.AdmissionOffers[0].OnlinePrice);
        Assert.Null(result.Value.AdmissionOffers[0].GatePrice);
        Assert.Null(result.Value.AdmissionOffers[1].OnlinePrice);
        Assert.NotNull(result.Value.AdmissionOffers[1].GatePrice);
    }

    [Fact]
    public void Normalize_ShouldRejectInvertedValidityDatesIndependentlyOfPriceMode()
    {
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            ParkingOffers = new List<ParkParkingPriceOffer>
            {
                new ParkParkingPriceOffer
                {
                    Code = "seasonal-parking",
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Dynamic },
                    ValidFrom = new DateOnly(2026, 9, 1),
                    ValidTo = new DateOnly(2026, 8, 31),
                },
            },
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors.SelectMany(static error => error.Details ?? new Dictionary<string, IReadOnlyCollection<string>>()),
            static detail => detail.Value.Contains("invalid-date-range"));
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
