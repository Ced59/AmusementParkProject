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
