using AmusementPark.Core.Domain.Users;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Users;

public sealed class PublicDisplayNamePolicyTests
{
    [Theory]
    [InlineData("Admin01")]
    [InlineData("Ａｄｍｉｎ01")]
    [InlineData("𝐀𝐝𝐦𝐢𝐧01")]
    [InlineData("@dmin")]
    [InlineData("Adm!n")]
    [InlineData("Official")]
    [InlineData("Support")]
    [InlineData("Équipe")]
    [InlineData("Mοdο42")]
    public void IsReserved_WhenRoleOrBrandVariantProvided_ShouldReturnTrue(string value)
    {
        Assert.True(PublicDisplayNamePolicy.IsReserved(value));
    }

    [Theory]
    [InlineData("\u200B")]
    [InlineData("Alice\u200B")]
    [InlineData("\u202EecilA")]
    [InlineData("...")]
    public void IsValid_WhenInvisibleOrContentlessValueProvided_ShouldReturnFalse(string value)
    {
        Assert.False(PublicDisplayNamePolicy.IsValid(value));
    }

    [Theory]
    [InlineData("CoasterFan")]
    [InlineData("Alice-42")]
    [InlineData("🎢")]
    public void IsValid_WhenVisibleNicknameProvided_ShouldReturnTrue(string value)
    {
        Assert.True(PublicDisplayNamePolicy.IsValid(value));
    }
}
