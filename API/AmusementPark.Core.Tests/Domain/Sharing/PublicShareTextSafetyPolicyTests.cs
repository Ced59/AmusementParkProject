using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class PublicShareTextSafetyPolicyTests
{
    [Theory]
    [InlineData("Une journée mémorable entre amis")]
    [InlineData("Des loopings, des rires et beaucoup de souvenirs !")]
    [InlineData("")]
    public void IsSafePlainText_WithEditorialText_ShouldReturnTrue(string value)
    {
        Assert.True(PublicShareTextSafetyPolicy.IsSafePlainText(value));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://example.test")]
    [InlineData("www.example.test")]
    public void IsSafePlainText_WithMarkupOrLinks_ShouldReturnFalse(string value)
    {
        Assert.False(PublicShareTextSafetyPolicy.IsSafePlainText(value));
    }
}
