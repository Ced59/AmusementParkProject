using AmusementPark.Core.Localization;
using Xunit;

namespace AmusementPark.Core.Tests.Localization;

public sealed class LocalizedTextTests
{
    [Fact]
    public void Constructor_WhenEmptyConstructorIsUsed_ShouldInitializeLanguageAndValueDefaults()
    {
        LocalizedText text = new LocalizedText();

        Assert.Equal(string.Empty, text.LanguageCode);
        Assert.Null(text.Value);
    }

    [Fact]
    public void Constructor_WhenValuesAreProvided_ShouldExposeLanguageAndValue()
    {
        LocalizedText text = new LocalizedText("fr", "Bonjour");

        Assert.Equal("fr", text.LanguageCode);
        Assert.Equal("Bonjour", text.Value);
    }

    [Fact]
    public void Constructor_WhenNullValueIsProvided_ShouldKeepNullValue()
    {
        LocalizedText text = new LocalizedText("fr", null);

        Assert.Equal("fr", text.LanguageCode);
        Assert.Null(text.Value);
    }
}
