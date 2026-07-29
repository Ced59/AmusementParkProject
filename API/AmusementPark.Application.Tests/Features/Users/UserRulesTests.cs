using AmusementPark.Application.Features.Users;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class UserRulesTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(" USER@Example.COM ", "user@example.com")]
    public void NormalizeEmail_WhenValueProvided_ShouldTrimAndLowercaseOrReturnNull(string? email, string? expected)
    {
        string? result = UserRules.NormalizeEmail(email);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "EN")]
    [InlineData("", "EN")]
    [InlineData(" fr ", "FR")]
    [InlineData("en-us", "EN-US")]
    public void NormalizePreferredLanguage_WhenValueProvided_ShouldReturnUppercaseLanguage(string? preferredLanguage, string expected)
    {
        string result = UserRules.NormalizePreferredLanguage(preferredLanguage);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "Metric")]
    [InlineData("", "Metric")]
    [InlineData("   ", "Metric")]
    [InlineData("metric", "Metric")]
    [InlineData("Imperial", "Imperial")]
    [InlineData(" imperial ", "Imperial")]
    [InlineData("unknown", "Metric")]
    public void NormalizePreferredMeasurementSystem_WhenValueProvided_ShouldFallbackToMetric(string? preferredMeasurementSystem, string expected)
    {
        string result = UserRules.NormalizePreferredMeasurementSystem(preferredMeasurementSystem);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(" CoasterFan ", "CoasterFan")]
    public void NormalizePublicDisplayName_ShouldTrimOrReturnNull(string? value, string? expected)
    {
        string? result = UserRules.NormalizePublicDisplayName(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidPublicDisplayName_ShouldRejectInvalidLengthsAndInvisibleCharacters()
    {
        Assert.True(UserRules.IsValidPublicDisplayName(new string('a', 60)));
        Assert.False(UserRules.IsValidPublicDisplayName(new string('a', 61)));
        Assert.False(UserRules.IsValidPublicDisplayName("\u200B"));
    }

    [Theory]
    [InlineData("Admin01")]
    [InlineData("a-d_m i n 99")]
    [InlineData("Adm1nSupport")]
    [InlineData("Аdmіn01")]
    [InlineData("Ａｄｍｉｎ01")]
    [InlineData("𝐀𝐝𝐦𝐢𝐧01")]
    [InlineData("@dmin")]
    [InlineData("MODO42")]
    [InlineData("M0dérateur")]
    [InlineData("Mοdο42")]
    [InlineData("ModeratorTeam")]
    [InlineData("Staff")]
    [InlineData("Official")]
    [InlineData("Support")]
    [InlineData("Équipe")]
    [InlineData("Amusement Parks")]
    [InlineData("Amusement-Parks Support")]
    [InlineData("Équipe Amusement Parks")]
    [InlineData("User0042")]
    public void IsReservedPublicDisplayName_ShouldRejectStaffAndGeneratedIdentityVariants(string value)
    {
        Assert.True(UserRules.IsReservedPublicDisplayName(value));
    }

    [Theory]
    [InlineData("CoasterFan")]
    [InlineData("ModelPark")]
    [InlineData("Alice")]
    public void IsReservedPublicDisplayName_ShouldAllowOrdinaryNicknames(string value)
    {
        Assert.False(UserRules.IsReservedPublicDisplayName(value));
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("USER@EXAMPLE.COM", true)]
    [InlineData("user.name+tag@example.co.uk", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("missing-at.example.com", false)]
    [InlineData("missing-domain@", false)]
    [InlineData("a@b", false)]
    public void IsValidEmail_WhenValueProvided_ShouldReturnExpectedValidity(string? email, bool expected)
    {
        bool result = UserRules.IsValidEmail(email);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Aa1!aaaa", true)]
    [InlineData("A1!aaaaa", true)]
    [InlineData("aa1!aaaa", false)]
    [InlineData("AA1!AAAA", false)]
    [InlineData("Aa!aaaaa", false)]
    [InlineData("Aa1aaaaa", false)]
    [InlineData("Aa1!aaa", false)]
    [InlineData(null, false)]
    public void IsValidPassword_WhenValueProvided_ShouldReturnExpectedValidity(string? password, bool expected)
    {
        bool result = UserRules.IsValidPassword(password);

        Assert.Equal(expected, result);
    }
}
