using AmusementPark.Infrastructure.Services.Authentication;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Authentication;

public sealed class BcryptPasswordHasherTests
{
    [Fact]
    public void HashPassword_WhenPasswordProvided_ShouldReturnNonPlainTextHash()
    {
        BcryptPasswordHasher hasher = new BcryptPasswordHasher();

        string hash = hasher.HashPassword("P@ssw0rd!");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual("P@ssw0rd!", hash);
    }

    [Fact]
    public void VerifyPassword_WhenPasswordMatchesHash_ShouldReturnTrue()
    {
        BcryptPasswordHasher hasher = new BcryptPasswordHasher();
        string hash = hasher.HashPassword("P@ssw0rd!");

        bool result = hasher.VerifyPassword("P@ssw0rd!", hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WhenPasswordDoesNotMatchHash_ShouldReturnFalse()
    {
        BcryptPasswordHasher hasher = new BcryptPasswordHasher();
        string hash = hasher.HashPassword("P@ssw0rd!");

        bool result = hasher.VerifyPassword("other", hash);

        Assert.False(result);
    }
}
