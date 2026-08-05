using System.Net.Http.Headers;
using System.Text.Json;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Infrastructure.Configuration.SocialPublishing;

namespace AmusementPark.Infrastructure.Services.SocialPublishing;

public sealed class FacebookPageSocialPublisher : ISocialPublisher
{
    public const string HttpClientName = "SocialPublishing.Facebook";

    private const string GraphApiBaseUrl = "https://graph.facebook.com";
    private const int MaximumStoredErrorLength = 500;

    private readonly IHttpClientFactory httpClientFactory;
    private readonly FacebookPagePublishingSettings settings;

    public FacebookPageSocialPublisher(
        IHttpClientFactory httpClientFactory,
        FacebookPagePublishingSettings settings)
    {
        this.httpClientFactory = httpClientFactory;
        this.settings = settings;
    }

    public SocialNetwork Network => SocialNetwork.Facebook;

    public SocialPublisherDescriptor Describe()
    {
        return new SocialPublisherDescriptor(
            this.Network,
            "Facebook",
            this.settings.Enabled,
            this.settings.IsConfigured(),
            string.IsNullOrWhiteSpace(this.settings.PageUrl) ? null : this.settings.PageUrl,
            true);
    }

    public async Task<SocialPublisherResult> PublishLinkAsync(
        SocialPublisherRequest request,
        CancellationToken cancellationToken)
    {
        if (!this.settings.IsConfigured())
        {
            return Failure("publisher-not-configured", "La Page Facebook n'est pas configurée.");
        }

        string endpoint = $"{GraphApiBaseUrl}/{this.settings.ApiVersion}/{Uri.EscapeDataString(this.settings.PageId)}/feed";
        using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.settings.PageAccessToken);
        httpRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["message"] = request.Message,
            ["link"] = request.Url,
        });

        HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
        using HttpResponseMessage response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ParseFailure(responseBody, ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        string? externalPostId = ParsePostId(responseBody);
        if (string.IsNullOrWhiteSpace(externalPostId))
        {
            return Failure("facebook-invalid-response", "Facebook n'a pas renvoyé d'identifiant de publication.");
        }

        return new SocialPublisherResult(
            true,
            externalPostId,
            BuildExternalPostUrl(externalPostId),
            null,
            null);
    }

    private static string BuildExternalPostUrl(string externalPostId)
    {
        string[] parts = externalPostId.Split('_', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            return $"https://www.facebook.com/{Uri.EscapeDataString(parts[0])}/posts/{Uri.EscapeDataString(parts[1])}";
        }

        return $"https://www.facebook.com/{Uri.EscapeDataString(externalPostId)}";
    }

    private static string? ParsePostId(string responseBody)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBody);
            return document.RootElement.TryGetProperty("id", out JsonElement idElement)
                && idElement.ValueKind == JsonValueKind.String
                    ? idElement.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static SocialPublisherResult ParseFailure(string responseBody, string fallbackCode)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("error", out JsonElement errorElement)
                || errorElement.ValueKind != JsonValueKind.Object)
            {
                return Failure(fallbackCode, "Facebook a refusé la publication.");
            }

            string code = errorElement.TryGetProperty("code", out JsonElement codeElement)
                ? codeElement.ToString()
                : fallbackCode;
            if (errorElement.TryGetProperty("error_subcode", out JsonElement subcodeElement))
            {
                code = $"{code}/{subcodeElement}";
            }

            string message = errorElement.TryGetProperty("message", out JsonElement messageElement)
                && messageElement.ValueKind == JsonValueKind.String
                    ? messageElement.GetString() ?? "Facebook a refusé la publication."
                    : "Facebook a refusé la publication.";
            return Failure(code, message);
        }
        catch (JsonException)
        {
            return Failure(fallbackCode, "Facebook a refusé la publication.");
        }
    }

    private static SocialPublisherResult Failure(string code, string message)
    {
        string safeMessage = message.Length <= MaximumStoredErrorLength
            ? message
            : message[..MaximumStoredErrorLength];
        return new SocialPublisherResult(false, null, null, code, safeMessage);
    }
}
