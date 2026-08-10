using AmusementPark.Application.Errors;
using AmusementPark.Core.Localization;
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
}
