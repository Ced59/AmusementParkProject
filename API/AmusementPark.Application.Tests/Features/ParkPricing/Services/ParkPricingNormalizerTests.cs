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
    [InlineData("EUO")]
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
                    Labels = CreateLocalizedTexts("Adult"),
                    GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 59m },
                },
                new ParkAdmissionPriceOffer
                {
                    Code = "child-range",
                    AudienceCategory = "child",
                    Labels = CreateLocalizedTexts("Child"),
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Range, MinimumAmount = 29m, MaximumAmount = 45m },
                },
                new ParkAdmissionPriceOffer
                {
                    Code = "dynamic",
                    AudienceCategory = "adult",
                    Labels = CreateLocalizedTexts("Dynamic"),
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
                    Names = CreateLocalizedTexts("Gold pass"),
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
                    Labels = CreateLocalizedTexts("Parking"),
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
                    Labels = CreateLocalizedTexts("Online only"),
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 39m },
                },
                new ParkAdmissionPriceOffer
                {
                    Code = "gate-only",
                    AudienceCategory = "child",
                    Labels = CreateLocalizedTexts("Gate only"),
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
                    Labels = CreateLocalizedTexts("Seasonal parking"),
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

    [Fact]
    public void Normalize_ShouldRequireAllPublicLanguagesForVisitorFacingTexts()
    {
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            Notes = new List<LocalizedText> { new LocalizedText("fr", "Note") },
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                new ParkAdmissionPriceOffer
                {
                    Code = "adult",
                    AudienceCategory = "adult",
                    Labels = new List<LocalizedText> { new LocalizedText("fr", "Adulte") },
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 39m },
                    Conditions = new List<LocalizedText> { new LocalizedText("fr", "Billet daté.") },
                },
            },
            AnnualPasses = new List<ParkAnnualPassOffer>
            {
                new ParkAnnualPassOffer
                {
                    Code = "gold",
                    Names = new List<LocalizedText> { new LocalizedText("fr", "Pass Or") },
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 199m },
                },
            },
            ParkingOffers = new List<ParkParkingPriceOffer>
            {
                new ParkParkingPriceOffer
                {
                    Code = "car",
                    Labels = new List<LocalizedText> { new LocalizedText("fr", "Voiture") },
                    GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 15m },
                },
            },
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);
        IReadOnlyCollection<KeyValuePair<string, IReadOnlyCollection<string>>> details = result.Errors
            .SelectMany(static error => error.Details ?? new Dictionary<string, IReadOnlyCollection<string>>())
            .ToList();

        Assert.False(result.IsSuccess);
        Assert.Contains(details, static detail => detail.Key == "Notes" && detail.Value.Contains("missing-language:en"));
        Assert.Contains(details, static detail => detail.Key == "AdmissionOffers[0].labels" && detail.Value.Contains("missing-language:en"));
        Assert.Contains(details, static detail => detail.Key == "AdmissionOffers[0].conditions" && detail.Value.Contains("missing-language:en"));
        Assert.Contains(details, static detail => detail.Key == "AnnualPasses[0].names" && detail.Value.Contains("missing-language:en"));
        Assert.Contains(details, static detail => detail.Key == "ParkingOffers[0].labels" && detail.Value.Contains("missing-language:en"));
    }

    [Theory]
    [InlineData("SourceUrl", "https//prices.example.test")]
    [InlineData("PurchaseUrl", "javascript:alert(1)")]
    [InlineData("PurchaseUrl", "http:tickets.example.test")]
    [InlineData("AdmissionOffers[0].purchaseUrl", "ftp://tickets.example.test/admission")]
    [InlineData("AnnualPasses[0].purchaseUrl", "/passes/gold")]
    [InlineData("ParkingOffers[0].purchaseUrl", "tickets.example.test/parking")]
    public void Normalize_ShouldRejectMalformedOrNonHttpUrls(string expectedField, string invalidUrl)
    {
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            SourceUrl = expectedField == "SourceUrl" ? invalidUrl : "https://park.example.test/prices",
            PurchaseUrl = expectedField == "PurchaseUrl" ? invalidUrl : "https://park.example.test/tickets",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmission("adult", "adult", 39m, null, null),
            },
            AnnualPasses = new List<ParkAnnualPassOffer>
            {
                new ParkAnnualPassOffer
                {
                    Code = "gold",
                    Names = CreateLocalizedTexts("Gold"),
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 199m },
                },
            },
            ParkingOffers = new List<ParkParkingPriceOffer>
            {
                new ParkParkingPriceOffer
                {
                    Code = "car",
                    Labels = CreateLocalizedTexts("Car"),
                    GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 15m },
                },
            },
        };
        pricing.AdmissionOffers[0].PurchaseUrl = expectedField == "AdmissionOffers[0].purchaseUrl"
            ? invalidUrl
            : "https://park.example.test/admission";
        pricing.AnnualPasses[0].PurchaseUrl = expectedField == "AnnualPasses[0].purchaseUrl"
            ? invalidUrl
            : "https://park.example.test/passes";
        pricing.ParkingOffers[0].PurchaseUrl = expectedField == "ParkingOffers[0].purchaseUrl"
            ? invalidUrl
            : "https://park.example.test/parking";

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);
        IReadOnlyCollection<KeyValuePair<string, IReadOnlyCollection<string>>> details = result.Errors
            .SelectMany(static error => error.Details ?? new Dictionary<string, IReadOnlyCollection<string>>())
            .ToList();

        Assert.False(result.IsSuccess);
        Assert.Contains(details, detail => detail.Key == expectedField && detail.Value.Contains("invalid-http-url"));
    }

    [Fact]
    public void Normalize_ShouldPreserveHistoricalSnapshotsWithTheirOwnCurrenciesAndStableProductCodes()
    {
        ParkPricingEntity pricing = new()
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmission("adult", "adult", 49m, null, null),
            },
            HistoricalSnapshots = new List<ParkPricingSnapshot>
            {
                CreateHistoricalSnapshot(2024, "hrk", 300m),
                CreateHistoricalSnapshot(2025, "eur", 42m),
            },
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Collection(
            result.Value.HistoricalSnapshots,
            snapshot =>
            {
                Assert.Equal(2025, snapshot.Year);
                Assert.Equal("EUR", snapshot.CurrencyCode);
                Assert.Equal("adult", Assert.Single(snapshot.AdmissionOffers).Code);
            },
            snapshot =>
            {
                Assert.Equal(2024, snapshot.Year);
                Assert.Equal("HRK", snapshot.CurrencyCode);
                Assert.Equal("adult", Assert.Single(snapshot.AdmissionOffers).Code);
            });
    }

    [Fact]
    public void Normalize_ShouldRejectDuplicateHistoricalYears()
    {
        ParkPricingEntity pricing = new()
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            HistoricalSnapshots = new List<ParkPricingSnapshot>
            {
                CreateHistoricalSnapshot(2025, "EUR", 39m),
                CreateHistoricalSnapshot(2025, "EUR", 42m),
            },
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors.SelectMany(static error => error.Details ?? new Dictionary<string, IReadOnlyCollection<string>>()),
            static detail => detail.Key == "HistoricalSnapshots[1].Year" && detail.Value.Contains("duplicate"));
    }

    [Fact]
    public void Normalize_ShouldRejectHistoricalSnapshotWithoutPricedOffers()
    {
        ParkPricingEntity pricing = new()
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            HistoricalSnapshots = new List<ParkPricingSnapshot>
            {
                new ParkPricingSnapshot { Year = 2025, CurrencyCode = "EUR" },
            },
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors.SelectMany(static error => error.Details ?? new Dictionary<string, IReadOnlyCollection<string>>()),
            static detail => detail.Key == "HistoricalSnapshots[0].Offers" && detail.Value.Contains("priced-offer-required"));
    }

    [Fact]
    public void Normalize_ShouldRejectInvalidHistoricalCurrencyWithoutChangingCurrentCurrency()
    {
        ParkPricingEntity pricing = new()
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            HistoricalSnapshots = new List<ParkPricingSnapshot>
            {
                CreateHistoricalSnapshot(2025, "EURO", 39m),
            },
        };

        ApplicationResult<ParkPricingEntity> result = ParkPricingNormalizer.Normalize(pricing);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors.SelectMany(static error => error.Details ?? new Dictionary<string, IReadOnlyCollection<string>>()),
            static detail => detail.Key == "HistoricalSnapshots[0].CurrencyCode" && detail.Value.Contains("invalid-iso-4217-code"));
    }

    private static ParkAdmissionPriceOffer CreateAdmission(string code, string audience, decimal amount, DateOnly? validFrom, DateOnly? validTo)
    {
        return new ParkAdmissionPriceOffer
        {
            Code = code,
            AudienceCategory = audience,
            Labels = CreateLocalizedTexts(code),
            GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = amount },
            ValidFrom = validFrom,
            ValidTo = validTo,
        };
    }

    private static ParkPricingSnapshot CreateHistoricalSnapshot(int year, string currencyCode, decimal amount)
    {
        return new ParkPricingSnapshot
        {
            Year = year,
            CurrencyCode = currencyCode,
            SourceUrl = $"https://example.test/prices/{year}",
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                CreateAdmission("adult", "adult", amount, null, null),
            },
        };
    }

    private static List<LocalizedText> CreateLocalizedTexts(string value)
    {
        return new[] { "fr", "en", "es", "de", "it", "nl", "pt", "pl" }
            .Select(languageCode => new LocalizedText(languageCode, value))
            .ToList();
    }
}
