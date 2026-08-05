using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Infrastructure.Configuration.SocialPublishing;
using AmusementPark.Infrastructure.Services.SocialPublishing;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.SocialPublishing;

public sealed class FacebookPageWebhookHandlerTests
{
    [Fact]
    public void VerifySignature_WhenPayloadIsAuthentic_ShouldAcceptIt()
    {
        FacebookPageWebhookHandler handler = new FacebookPageWebhookHandler(CreateSettings());
        const string payload = "{\"object\":\"page\",\"entry\":[]}";
        string signature = CreateSignature(payload, "app-secret");

        bool result = handler.VerifySignature(payload, signature);

        Assert.True(result);
    }

    [Fact]
    public void ParseChanges_WhenFeedContainsEditAndDelete_ShouldReturnTrackedEvents()
    {
        FacebookPageWebhookHandler handler = new FacebookPageWebhookHandler(CreateSettings());
        const string payload = """
            {
              "object": "page",
              "entry": [{
                "changes": [
                  { "field": "feed", "value": { "verb": "edited", "post_id": "123_456", "message": "Updated" } },
                  { "field": "feed", "value": { "verb": "remove", "post_id": "123_789" } },
                  { "field": "feed", "value": { "verb": "add", "post_id": "123_999" } }
                ]
              }]
            }
            """;

        IReadOnlyCollection<SocialWebhookChange> changes = handler.ParseChanges(payload);

        Assert.Collection(
            changes,
            change =>
            {
                Assert.Equal("123_456", change.ExternalPostId);
                Assert.Equal(SocialWebhookChangeKind.Updated, change.Kind);
                Assert.Equal("Updated", change.Message);
            },
            change =>
            {
                Assert.Equal("123_789", change.ExternalPostId);
                Assert.Equal(SocialWebhookChangeKind.Deleted, change.Kind);
            });
    }

    private static FacebookPagePublishingSettings CreateSettings()
    {
        return new FacebookPagePublishingSettings
        {
            Enabled = true,
            PageId = "123",
            PageAccessToken = "page-token",
            PageUrl = "https://www.facebook.com/test",
            WebhookEnabled = true,
            AppSecret = "app-secret",
            WebhookVerifyToken = "verify-token",
        };
    }

    private static string CreateSignature(string payload, string secret)
    {
        using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
