using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Infrastructure.Configuration.SocialPublishing;

namespace AmusementPark.Infrastructure.Services.SocialPublishing;

public sealed class FacebookPageWebhookHandler : ISocialWebhookHandler
{
    private const int MaximumPayloadLength = 262144;

    private readonly FacebookPagePublishingSettings settings;

    public FacebookPageWebhookHandler(FacebookPagePublishingSettings settings)
    {
        this.settings = settings;
    }

    public SocialNetwork Network => SocialNetwork.Facebook;

    public bool IsEnabled => this.settings.IsWebhookConfigured();

    public bool VerifySubscriptionToken(string? verifyToken)
    {
        return this.IsEnabled && FixedTimeEquals(this.settings.WebhookVerifyToken, verifyToken);
    }

    public bool VerifySignature(string payload, string? signature)
    {
        if (!this.IsEnabled
            || payload.Length > MaximumPayloadLength
            || string.IsNullOrWhiteSpace(signature)
            || !signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        byte[] suppliedSignature;
        try
        {
            suppliedSignature = Convert.FromHexString(signature[7..]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] secretBytes = Encoding.UTF8.GetBytes(this.settings.AppSecret);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        using HMACSHA256 hmac = new HMACSHA256(secretBytes);
        byte[] expectedSignature = hmac.ComputeHash(payloadBytes);
        return CryptographicOperations.FixedTimeEquals(expectedSignature, suppliedSignature);
    }

    public IReadOnlyCollection<SocialWebhookChange> ParseChanges(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload) || payload.Length > MaximumPayloadLength)
        {
            return Array.Empty<SocialWebhookChange>();
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;
            if (!root.TryGetProperty("object", out JsonElement objectElement)
                || !string.Equals(objectElement.GetString(), "page", StringComparison.OrdinalIgnoreCase)
                || !root.TryGetProperty("entry", out JsonElement entries)
                || entries.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<SocialWebhookChange>();
            }

            List<SocialWebhookChange> changes = new List<SocialWebhookChange>();
            foreach (JsonElement entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("changes", out JsonElement entryChanges)
                    || entryChanges.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement change in entryChanges.EnumerateArray())
                {
                    SocialWebhookChange? parsedChange = ParseChange(change);
                    if (parsedChange is not null)
                    {
                        changes.Add(parsedChange);
                    }
                }
            }

            return changes;
        }
        catch (JsonException)
        {
            return Array.Empty<SocialWebhookChange>();
        }
    }

    private static SocialWebhookChange? ParseChange(JsonElement change)
    {
        if (!change.TryGetProperty("field", out JsonElement fieldElement)
            || !string.Equals(fieldElement.GetString(), "feed", StringComparison.OrdinalIgnoreCase)
            || !change.TryGetProperty("value", out JsonElement value)
            || value.ValueKind != JsonValueKind.Object
            || !value.TryGetProperty("post_id", out JsonElement postIdElement))
        {
            return null;
        }

        string externalPostId = postIdElement.GetString()?.Trim() ?? string.Empty;
        string verb = value.TryGetProperty("verb", out JsonElement verbElement)
            ? verbElement.GetString()?.Trim().ToLowerInvariant() ?? string.Empty
            : string.Empty;
        if (externalPostId.Length == 0)
        {
            return null;
        }

        SocialWebhookChangeKind? kind = verb switch
        {
            "delete" or "remove" => SocialWebhookChangeKind.Deleted,
            "edit" or "edited" or "update" => SocialWebhookChangeKind.Updated,
            _ => null,
        };
        if (kind is null)
        {
            return null;
        }

        string? message = value.TryGetProperty("message", out JsonElement messageElement)
            && messageElement.ValueKind == JsonValueKind.String
                ? messageElement.GetString()
                : null;
        return new SocialWebhookChange(externalPostId, kind.Value, message);
    }

    private static bool FixedTimeEquals(string expected, string? supplied)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] suppliedBytes = Encoding.UTF8.GetBytes(supplied ?? string.Empty);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
