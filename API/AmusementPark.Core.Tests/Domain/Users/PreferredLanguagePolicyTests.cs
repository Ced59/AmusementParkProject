using AmusementPark.Core.Domain.Users;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Users;

public sealed class PreferredLanguagePolicyTests
{
    [Theory]
    [InlineData("fr", "FR")]
    [InlineData(" PT ", "PT")]
    [InlineData("nl", "NL")]
    public void TryNormalize_WhenLanguageIsSupported_ShouldReturnUppercaseCode(
        string language,
        string expected)
    {
        bool success = PreferredLanguagePolicy.TryNormalize(language, out string normalizedLanguage);

        Assert.True(success);
        Assert.Equal(expected, normalizedLanguage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("en-US")]
    [InlineData("ja")]
    public void TryNormalize_WhenLanguageIsUnsupported_ShouldRejectIt(string? language)
    {
        bool success = PreferredLanguagePolicy.TryNormalize(language, out _);

        Assert.False(success);
    }
}
