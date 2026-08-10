using AmusementPark.Application.Errors;
using AmusementPark.Core.Localization;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkPricing;
using AmusementPark.WebAPI.Mappers;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class ParkPricingHttpMappersTests
{
    [Fact]
    public void ToDomainResult_WhenLocalizedNotesAreSubmitted_ShouldMapThem()
    {
        ParkPricingDto dto = new()
        {
            CurrencyCode = "EUR",
            Notes = new[]
            {
                new LocalizedTextDto { LanguageCode = "fr", Value = "Tarifs indicatifs." },
                new LocalizedTextDto { LanguageCode = "en", Value = "Indicative prices." },
            },
        };

        ApplicationResult<ParkPricingEntity> result = dto.ToDomainResult("park-1");

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value!.Notes,
            static note =>
            {
                Assert.Equal("fr", note.LanguageCode);
                Assert.Equal("Tarifs indicatifs.", note.Value);
            },
            static note =>
            {
                Assert.Equal("en", note.LanguageCode);
                Assert.Equal("Indicative prices.", note.Value);
            });
    }

    [Fact]
    public void ToHttp_WhenLocalizedNotesExist_ShouldPreserveThem()
    {
        ParkPricingEntity pricing = new()
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            Notes = new List<LocalizedText>
            {
                new("fr", "Tarifs indicatifs."),
                new("pl", "Ceny orientacyjne."),
            },
        };

        ParkPricingDto dto = pricing.ToHttp();

        Assert.Collection(
            dto.Notes,
            static note =>
            {
                Assert.Equal("fr", note.LanguageCode);
                Assert.Equal("Tarifs indicatifs.", note.Value);
            },
            static note =>
            {
                Assert.Equal("pl", note.LanguageCode);
                Assert.Equal("Ceny orientacyjne.", note.Value);
            });
    }

    [Fact]
    public void ToDomainResult_WhenHistoricalSnapshotsAreSubmitted_ShouldPreserveYearCurrencyAndOffers()
    {
        ParkPricingDto dto = new()
        {
            CurrencyCode = "EUR",
            HistoricalSnapshots = new[]
            {
                new ParkPricingSnapshotDto
                {
                    Year = 2024,
                    CurrencyCode = "HRK",
                    AdmissionOffers = new[]
                    {
                        new ParkAdmissionPriceOfferDto
                        {
                            Code = "adult",
                            AudienceCategory = "adult",
                            OnlinePrice = new ParkPriceValueDto { Mode = "Fixed", Amount = 300m },
                        },
                    },
                },
            },
        };

        ApplicationResult<ParkPricingEntity> result = dto.ToDomainResult("park-1");

        Assert.True(result.IsSuccess);
        ParkPricingSnapshot snapshot = Assert.Single(result.Value!.HistoricalSnapshots);
        Assert.Equal(2024, snapshot.Year);
        Assert.Equal("HRK", snapshot.CurrencyCode);
        Assert.Equal(300m, Assert.Single(snapshot.AdmissionOffers).OnlinePrice!.Amount);
    }

    [Fact]
    public void ToPublicHttp_ShouldLimitHistoryWithoutChangingAdminMapping()
    {
        ParkPricingEntity pricing = new()
        {
            ParkId = "park-1",
            CurrencyCode = "EUR",
            HistoricalSnapshots = Enumerable.Range(2000, 15)
                .Select(static year => new ParkPricingSnapshot { Year = year, CurrencyCode = "EUR" })
                .ToList(),
        };

        ParkPricingDto publicDto = pricing.ToPublicHttp();
        ParkPricingDto adminDto = pricing.ToHttp();

        Assert.NotNull(publicDto.HistoricalSnapshots);
        Assert.NotNull(adminDto.HistoricalSnapshots);
        Assert.Equal(10, publicDto.HistoricalSnapshots.Count);
        Assert.Equal(2014, publicDto.HistoricalSnapshots.First().Year);
        Assert.Equal(15, adminDto.HistoricalSnapshots.Count);
    }
}
