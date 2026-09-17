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
        int tamperedIndex = token.Length / 2;
        char replacement = token[tamperedIndex] == 'a' ? 'b' : 'a';
        string tampered = token[..tamperedIndex]
            + replacement
            + token[(tamperedIndex + 1)..];

        bool success = protector.TryReadUserId(tampered, out string userId);

        Assert.False(success);
        Assert.Empty(userId);
    }

    [Fact]
    public void TryReadUserId_ShouldRejectANonCanonicalBase64UrlAlias()
    {
        const string alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        EncryptedNotificationEmailUnsubscribeTokenProtector protector = CreateProtector();
        string token = protector.CreateToken("user-1");
        byte[] expectedPayload = DecodeBase64Url(token);
        char replacement = alphabet.First(candidate =>
            candidate != token[^1]
            && DecodeBase64Url(token[..^1] + candidate).SequenceEqual(expectedPayload));
        string nonCanonicalAlias = token[..^1] + replacement;

        bool success = protector.TryReadUserId(nonCanonicalAlias, out string userId);

        Assert.False(success);
        Assert.Empty(userId);
    }

    [Fact]
    public void TryReadUserId_ShouldRejectSurroundingWhitespace()
    {
        EncryptedNotificationEmailUnsubscribeTokenProtector protector = CreateProtector();
        string token = protector.CreateToken("user-1");

        bool leadingSuccess = protector.TryReadUserId(" " + token, out string leadingUserId);
        bool trailingSuccess = protector.TryReadUserId(token + " ", out string trailingUserId);

        Assert.False(leadingSuccess);
        Assert.Empty(leadingUserId);
        Assert.False(trailingSuccess);
        Assert.Empty(trailingUserId);
    }

    private static EncryptedNotificationEmailUnsubscribeTokenProtector CreateProtector()
    {
        return new EncryptedNotificationEmailUnsubscribeTokenProtector(new JwtSettings
        {
            Key = "a-test-key-that-is-long-enough-for-encryption-protection",
        });
    }

    private static byte[] DecodeBase64Url(string value)
    {
        string base64 = value.Replace('-', '+').Replace('_', '/');
        int padding = (4 - base64.Length % 4) % 4;
        return Convert.FromBase64String(base64.PadRight(base64.Length + padding, '='));
    }
}
