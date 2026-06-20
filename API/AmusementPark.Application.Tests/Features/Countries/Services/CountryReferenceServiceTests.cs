using AmusementPark.Application.Features.Countries;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.Countries.Services;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Countries.Services;

public sealed class CountryReferenceServiceTests
{
    [Theory]
    [InlineData(WorldRegionFilter.Europe, "FR")]
    [InlineData(WorldRegionFilter.NorthAmerica, "US")]
    [InlineData(WorldRegionFilter.SouthAmerica, "BR")]
    [InlineData(WorldRegionFilter.Asia, "JP")]
    [InlineData(WorldRegionFilter.MiddleEast, "AE")]
    [InlineData(WorldRegionFilter.Oceania, "AU")]
    [InlineData(WorldRegionFilter.Orient, "AU")]
    [InlineData(WorldRegionFilter.Africa, "ZA")]
    public void GetCountryCodesForRegion_WhenKnownRegionProvided_ShouldContainRepresentativeCountry(WorldRegionFilter region, string expectedCountryCode)
    {
        CountryReferenceService service = new CountryReferenceService(Mock.Of<ICountryReadRepository>());

        IReadOnlyCollection<string> result = service.GetCountryCodesForRegion(region);

        Assert.Contains(expectedCountryCode, result);
        Assert.DoesNotContain(result, static countryCode => countryCode.Length != 2);
    }

    [Fact]
    public void GetCountryCodesForRegion_WhenEuropeProvided_ShouldNotContainRussia()
    {
        CountryReferenceService service = new CountryReferenceService(Mock.Of<ICountryReadRepository>());

        IReadOnlyCollection<string> result = service.GetCountryCodesForRegion(WorldRegionFilter.Europe);

        Assert.DoesNotContain("RU", result);
    }

    [Fact]
    public void GetCountryCodesForRegion_WhenAsiaProvided_ShouldContainRussiaAndExcludeAustralia()
    {
        CountryReferenceService service = new CountryReferenceService(Mock.Of<ICountryReadRepository>());

        IReadOnlyCollection<string> result = service.GetCountryCodesForRegion(WorldRegionFilter.Asia);

        Assert.Contains("RU", result);
        Assert.DoesNotContain("AU", result);
    }

    [Fact]
    public void GetCountryCodesForRegion_WhenOceaniaProvided_ShouldContainAustralia()
    {
        CountryReferenceService service = new CountryReferenceService(Mock.Of<ICountryReadRepository>());

        IReadOnlyCollection<string> result = service.GetCountryCodesForRegion(WorldRegionFilter.Oceania);

        Assert.Contains("AU", result);
        Assert.DoesNotContain("RU", result);
    }

    [Fact]
    public void GetCountryCodesForRegion_WhenRegionIsNull_ShouldReturnEmptyCollection()
    {
        CountryReferenceService service = new CountryReferenceService(Mock.Of<ICountryReadRepository>());

        IReadOnlyCollection<string> result = service.GetCountryCodesForRegion(null);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindCountryCodesByLocalizedSearchAsync_WhenSearchIsBlank_ShouldReturnEmptyAndNotCallRepository(string? searchTerm)
    {
        Mock<ICountryReadRepository> repository = new Mock<ICountryReadRepository>(MockBehavior.Strict);
        CountryReferenceService service = new CountryReferenceService(repository.Object);

        IReadOnlyCollection<string> result = await service.FindCountryCodesByLocalizedSearchAsync(searchTerm, CancellationToken.None);

        Assert.Empty(result);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FindCountryCodesByLocalizedSearchAsync_WhenIsoCodeMatches_ShouldReturnNormalizedDistinctCodes()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        Country[] countries = new[]
        {
            new Country { IsoCode = " fr ", Names = new List<LocalizedText> { new LocalizedText("fr", "France") } },
            new Country { IsoCode = "FR", Names = new List<LocalizedText> { new LocalizedText("en", "French Republic") } },
            new Country { IsoCode = "BE", Names = new List<LocalizedText> { new LocalizedText("fr", "Belgique") } },
        };
        Mock<ICountryReadRepository> repository = new Mock<ICountryReadRepository>(MockBehavior.Strict);
        repository.Setup(item => item.GetAllAsync(null, cancellationToken)).ReturnsAsync(countries);
        CountryReferenceService service = new CountryReferenceService(repository.Object);

        IReadOnlyCollection<string> result = await service.FindCountryCodesByLocalizedSearchAsync("fr", cancellationToken);

        Assert.Equal(new[] { "FR" }, result);
        repository.VerifyAll();
    }

    [Fact]
    public async Task FindCountryCodesByLocalizedSearchAsync_WhenLocalizedNameMatchesIgnoringCaseAndAccents_ShouldReturnCode()
    {
        Country[] countries = new[]
        {
            new Country { IsoCode = "ES", Names = new List<LocalizedText> { new LocalizedText("fr", "Espagne") } },
            new Country { IsoCode = "DE", Names = new List<LocalizedText> { new LocalizedText("fr", "Allemagne") } },
        };
        Mock<ICountryReadRepository> repository = new Mock<ICountryReadRepository>(MockBehavior.Strict);
        repository.Setup(item => item.GetAllAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(countries);
        CountryReferenceService service = new CountryReferenceService(repository.Object);

        IReadOnlyCollection<string> result = await service.FindCountryCodesByLocalizedSearchAsync("espagne", CancellationToken.None);

        Assert.Equal(new[] { "ES" }, result);
    }
}
