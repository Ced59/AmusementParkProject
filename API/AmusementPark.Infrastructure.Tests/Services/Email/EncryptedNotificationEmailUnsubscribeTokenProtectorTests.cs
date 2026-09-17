using AmusementPark.Infrastructure.Configuration.Authentication;
using AmusementPark.Infrastructure.Services.Email;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Email;

public sealed class EncryptedNotificationEmailUnsubscribeTokenProtectorTests
{
    [Fact]
    public void CreateToken_ShouldRoundTripWithoutExposingTheTechnicalUserIdentifier()
    {
        EncryptedNotificationEmailUnsubscribeTokenProtector protector = CreateProtector();

        string token = protector.CreateToken("user-1");
        bool success = protector.TryReadUserId(token, out string userId);

        Assert.True(success);
        Assert.Equal("user-1", userId);
        Assert.DoesNotContain("user-1", token, StringComparison.Ordinal);
        Assert.DoesNotContain(
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("user-1")).TrimEnd('='),
            token,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CreateToken_ShouldUseANewNonceForEveryLink()
    {
        EncryptedNotificationEmailUnsubscribeTokenProtector protector = CreateProtector();

        string first = protector.CreateToken("user-1");
        string second = protector.CreateToken("user-1");

        Assert.NotEqual(first, second);
        Assert.True(protector.TryReadUserId(first, out string firstUserId));
        Assert.True(protector.TryReadUserId(second, out string secondUserId));
        Assert.Equal(firstUserId, secondUserId);
    }

    [Fact]
    public void TryReadUserId_ShouldRejectATamperedCiphertext()
    {
        EncryptedNotificationEmailUnsubscribeTokenProtector protector = CreateProtector();
        string token = protector.CreateToken("user-1");
        char replacement = token[^1] == 'a' ? 'b' : 'a';
        string tampered = token[..^1] + replacement;

        bool success = protector.TryReadUserId(tampered, out string userId);

        Assert.False(success);
        Assert.Empty(userId);
    }

    private static EncryptedNotificationEmailUnsubscribeTokenProtector CreateProtector()
    {
        return new EncryptedNotificationEmailUnsubscribeTokenProtector(new JwtSettings
        {
            Key = "a-test-key-that-is-long-enough-for-encryption-protection",
        });
    }
}
