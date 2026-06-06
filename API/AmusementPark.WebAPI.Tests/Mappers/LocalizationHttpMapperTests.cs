using AmusementPark.Core.Localization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class LocalizationHttpMapperTests
{
    [Fact]
    public void ToDomain_WhenValuesAreNull_ShouldReturnEmptyList()
    {
        List<LocalizedText> result = ((IEnumerable<LocalizedTextDto>?)null).ToDomain();

        Assert.Empty(result);
    }

    [Fact]
    public void ToDomain_WhenValuesContainInvalidAndDuplicateLanguages_ShouldTrimFilterAndKeepLastLanguageValue()
    {
        LocalizedTextDto[] values = new[]
        {
            new LocalizedTextDto { LanguageCode = " FR ", Value = " Ancien " },
            new LocalizedTextDto { LanguageCode = "", Value = "Ignored" },
            new LocalizedTextDto { LanguageCode = "en", Value = "   " },
            new LocalizedTextDto { LanguageCode = "fr", Value = " Nouveau " },
        };

        List<LocalizedText> result = values.ToDomain();

        LocalizedText text = Assert.Single(result);
        Assert.Equal("fr", text.LanguageCode);
        Assert.Equal("Nouveau", text.Value);
    }

    [Fact]
    public void ToHttp_WhenValuesAreNull_ShouldReturnEmptyList()
    {
        List<LocalizedTextDto> result = ((IEnumerable<LocalizedText>?)null).ToHttp();

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_WhenExactLanguageExists_ShouldReturnExactValue()
    {
        LocalizedText[] values = new[]
        {
            new LocalizedText("en", "Hello"),
            new LocalizedText("fr", "Bonjour"),
        };

        string result = values.Resolve("fr", "en");

        Assert.Equal("Bonjour", result);
    }

    [Fact]
    public void Resolve_WhenExactLanguageMissing_ShouldReturnDefaultLanguageValue()
    {
        LocalizedText[] values = new[]
        {
            new LocalizedText("en", "Hello"),
            new LocalizedText("de", "Hallo"),
        };

        string result = values.Resolve("fr", "en");

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Resolve_WhenDefaultLanguageMissing_ShouldReturnFirstNonBlankValue()
    {
        LocalizedText[] values = new[]
        {
            new LocalizedText("fr", "   "),
            new LocalizedText("de", "Hallo"),
        };

        string result = values.Resolve("es", "en");

        Assert.Equal("Hallo", result);
    }

    [Fact]
    public void Resolve_WhenValuesAreNull_ShouldReturnEmptyString()
    {
        string result = ((IEnumerable<LocalizedText>?)null).Resolve("fr");

        Assert.Equal(string.Empty, result);
    }
}
