using AmusementPark.Infrastructure.Services.Authentication;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Authentication;

public sealed class LocalAccountTokenFactoryTests
{
    [Fact]
    public void Generate_WhenCalled_ShouldReturnUrlSafeOpaqueToken()
    {
        LocalAccountTokenFactory factory = new LocalAccountTokenFactory();

        string token = factory.Generate();

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.DoesNotContain("+", token, StringComparison.Ordinal);
        Assert.DoesNotContain("/", token, StringComparison.Ordinal);
        Assert.DoesNotContain("=", token, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WhenCalledMultipleTimes_ShouldReturnDifferentTokens()
    {
        LocalAccountTokenFactory factory = new LocalAccountTokenFactory();

        string first = factory.Generate();
        string second = factory.Generate();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ComputeHash_WhenSameTokenProvided_ShouldReturnStableSha256HexHash()
    {
        LocalAccountTokenFactory factory = new LocalAccountTokenFactory();

        string first = factory.ComputeHash("token");
        string second = factory.ComputeHash("token");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.Equal(first.ToUpperInvariant(), first);
    }

    [Fact]
    public void ComputeHash_WhenDifferentTokensProvided_ShouldReturnDifferentHashes()
    {
        LocalAccountTokenFactory factory = new LocalAccountTokenFactory();

        string first = factory.ComputeHash("token-1");
        string second = factory.ComputeHash("token-2");

        Assert.NotEqual(first, second);
    }
}
