using System.Net.Http.Headers;
using System.Text.Json;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Infrastructure.Configuration.SocialPublishing;

namespace AmusementPark.Infrastructure.Services.SocialPublishing;

public sealed class FacebookPageSocialPublisher : ISocialPublisher
{
    public const string HttpClientName = "SocialPublishing.Facebook";

    public const string PreviewRefreshHttpClientName = "SocialPublishing.Facebook.PreviewRefresh";

    public const string PreviewPagePreparationHttpClientName = "SocialPublishing.Facebook.PreviewPagePreparation";

    private const string GraphApiBaseUrl = "https://graph.facebook.com";
    private const int MaximumStoredErrorLength = 500;
    private const int MaximumReconciliationPageCount = 20;

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IPublicSeoContextProvider publicSeoContextProvider;
    private readonly FacebookPagePublishingSettings settings;

    public FacebookPageSocialPublisher(
        IHttpClientFactory httpClientFactory,
        IPublicSeoContextProvider publicSeoContextProvider,
        FacebookPagePublishingSettings settings)
    {
        this.httpClientFactory = httpClientFactory;
        this.publicSeoContextProvider = publicSeoContextProvider;
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

        SocialPublisherOperationResult preparationResult = await this.PrepareLinkPreviewPageAsync(
            request.Url,
            cancellationToken);
        if (!preparationResult.IsSuccess)
        {
            return Failure(
                preparationResult.FailureCode ?? "preview-page-unavailable",
                preparationResult.FailureMessage ?? "La page publique n'est pas prête pour son aperçu Facebook.");
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

    public async Task<SocialPublisherLinkReconciliationResult> ReconcilePublishedLinkAsync(
        SocialPublisherLinkReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        if (!this.settings.IsConfigured())
        {
            return ReconciliationFailure(
                "publisher-not-configured",
                "La Page Facebook n'est pas configurée.");
        }

        DateTimeOffset attemptedAtUtc = new DateTimeOffset(
            DateTime.SpecifyKind(request.AttemptedAtUtc, DateTimeKind.Utc));
        long since = attemptedAtUtc.AddMinutes(-2).ToUnixTimeSeconds();
        long until = attemptedAtUtc.AddMinutes(10).ToUnixTimeSeconds();
        string fields = Uri.EscapeDataString("id,message,permalink_url,created_time");
        string baseEndpoint = $"{GraphApiBaseUrl}/{this.settings.ApiVersion}/{Uri.EscapeDataString(this.settings.PageId)}"
            + $"/published_posts?fields={fields}&since={since}&until={until}&limit=25";
        List<(string Id, string? Url)> matches = new List<(string Id, string? Url)>();
        string? afterCursor = null;

        try
        {
            for (int pageNumber = 1; pageNumber <= MaximumReconciliationPageCount; pageNumber++)
            {
                string endpoint = afterCursor is null
                    ? baseEndpoint
                    : $"{baseEndpoint}&after={Uri.EscapeDataString(afterCursor)}";
                using HttpRequestMessage httpRequest = this.CreateAuthorizedRequest(HttpMethod.Get, endpoint);
                HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
                using HttpResponseMessage response = await client.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    SocialPublisherResult failure = ParseFailure(
                        responseBody,
                        ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return ReconciliationFailure(
                        failure.FailureCode ?? "facebook-reconciliation-failed",
                        failure.FailureMessage ?? "Facebook n'a pas pu confirmer la publication.");
                }

                using JsonDocument document = JsonDocument.Parse(responseBody);
                if (!document.RootElement.TryGetProperty("data", out JsonElement dataElement)
                    || dataElement.ValueKind != JsonValueKind.Array)
                {
                    return ReconciliationFailure(
                        "facebook-invalid-response",
                        "Facebook a renvoyé une réponse de rapprochement invalide.");
                }

                matches.AddRange(dataElement
                    .EnumerateArray()
                    .Where(element => HasMatchingPublishedMessage(element, request.Message, attemptedAtUtc))
                    .Select(static element => (
                        element.GetProperty("id").GetString()!,
                        TryGetString(element, "permalink_url"))));
                if (matches.Count > 1)
                {
                    return AmbiguousReconciliation();
                }

                if (!TryGetNextCursor(document.RootElement, out bool hasNextPage, out afterCursor))
                {
                    return ReconciliationFailure(
                        "facebook-invalid-response",
                        "Facebook a renvoyé une pagination de rapprochement invalide.");
                }

                if (!hasNextPage)
                {
                    return BuildReconciliationResult(matches);
                }
            }

            return ReconciliationFailure(
                "facebook-reconciliation-page-limit",
                "Facebook a renvoyé trop de pages pour confirmer l'absence d'une publication en toute sécurité.");
        }
        catch (JsonException)
        {
            return ReconciliationFailure(
                "facebook-invalid-response",
                "Facebook a renvoyé une réponse de rapprochement invalide.");
        }
    }

    public async Task<SocialPublisherOperationResult> RefreshLinkPreviewAsync(
        string url,
        CancellationToken cancellationToken)
    {
        if (!this.settings.IsConfigured())
        {
            return new SocialPublisherOperationResult(
                false,
                false,
                "publisher-not-configured",
                "La Page Facebook n'est pas configurée.");
        }

        SocialPublisherOperationResult preparationResult = await this.PrepareLinkPreviewPageAsync(
            url,
            cancellationToken);
        if (!preparationResult.IsSuccess)
        {
            return preparationResult;
        }

        string endpoint = $"{GraphApiBaseUrl}/{this.settings.ApiVersion}/";
        using HttpRequestMessage httpRequest = this.CreateAuthorizedRequest(HttpMethod.Post, endpoint);
        httpRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = url,
            ["scrape"] = "true",
        });

        HttpClient client = this.httpClientFactory.CreateClient(PreviewRefreshHttpClientName);
        using HttpResponseMessage response = await client.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return new SocialPublisherOperationResult(true, false, null, null);
        }

        SocialPublisherResult failure = ParseFailure(
            responseBody,
            ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
        return new SocialPublisherOperationResult(
            false,
            false,
            failure.FailureCode,
            failure.FailureMessage);
    }

    private async Task<SocialPublisherOperationResult> PrepareLinkPreviewPageAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            PublicSeoContext context = await this.publicSeoContextProvider.GetAsync(cancellationToken);
            if (!IsPublicSiteUrl(url, context.PublicBaseUrl))
            {
                return new SocialPublisherOperationResult(true, false, null, null);
            }

            using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            httpRequest.Headers.UserAgent.ParseAdd("AmusementPark-SocialPreviewPrewarmer/1.0");
            httpRequest.Headers.TryAddWithoutValidation("X-AmusementPark-SSR-Warmup", "1");
            httpRequest.Headers.TryAddWithoutValidation("X-AmusementPark-SSR-Warmup-Refresh", "1");

            HttpClient client = this.httpClientFactory.CreateClient(PreviewPagePreparationHttpClientName);
            using HttpResponseMessage response = await client.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            await response.Content.CopyToAsync(Stream.Null, cancellationToken);
            bool isSeoReady = response.Headers.TryGetValues(
                    "X-AmusementPark-Seo-Ready",
                    out IEnumerable<string>? values)
                && values.Any(static value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
            if (response.IsSuccessStatusCode && isSeoReady)
            {
                return new SocialPublisherOperationResult(true, false, null, null);
            }

            return new SocialPublisherOperationResult(
                false,
                false,
                "preview-page-not-ready",
                "La page publique n'a pas pu être préparée pour son aperçu Facebook.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new SocialPublisherOperationResult(
                false,
                false,
                "preview-page-unavailable",
                "La page publique est temporairement indisponible pour son aperçu Facebook.");
        }
    }

    private static bool IsPublicSiteUrl(string url, string publicBaseUrl)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            && Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out Uri? publicBaseUri)
            && (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            && string.Equals(uri.Scheme, publicBaseUri.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(uri.Host, publicBaseUri.Host, StringComparison.OrdinalIgnoreCase)
            && uri.Port == publicBaseUri.Port;
    }

    public Task<SocialPublisherOperationResult> UpdatePostAsync(
        string externalPostId,
        string message,
        CancellationToken cancellationToken)
    {
        return this.SendMutationAsync(
            HttpMethod.Post,
            externalPostId,
            new Dictionary<string, string> { ["message"] = message },
            cancellationToken);
    }

    public Task<SocialPublisherOperationResult> DeletePostAsync(
        string externalPostId,
        CancellationToken cancellationToken)
    {
        return this.SendMutationAsync(HttpMethod.Delete, externalPostId, null, cancellationToken);
    }

    public async Task<SocialPublisherPostSnapshotResult> GetPostAsync(
        string externalPostId,
        CancellationToken cancellationToken)
    {
        if (!this.settings.IsConfigured())
        {
            return new SocialPublisherPostSnapshotResult(
                false,
                false,
                null,
                null,
                "publisher-not-configured",
                "La Page Facebook n'est pas configurée.");
        }

        string endpoint = $"{BuildObjectEndpoint(this.settings.ApiVersion, externalPostId)}?fields=id,message,permalink_url";
        using HttpRequestMessage httpRequest = this.CreateAuthorizedRequest(HttpMethod.Get, endpoint);
        HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
        using HttpResponseMessage response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound || IsMissingObject(responseBody))
            {
                return new SocialPublisherPostSnapshotResult(true, false, null, null, null, null);
            }

            SocialPublisherResult failure = ParseFailure(
                responseBody,
                ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
            return new SocialPublisherPostSnapshotResult(
                false,
                false,
                null,
                null,
                failure.FailureCode,
                failure.FailureMessage);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBody);
            JsonElement root = document.RootElement;
            string? message = root.TryGetProperty("message", out JsonElement messageElement)
                && messageElement.ValueKind == JsonValueKind.String
                    ? messageElement.GetString()
                    : null;
            string? permalink = root.TryGetProperty("permalink_url", out JsonElement permalinkElement)
                && permalinkElement.ValueKind == JsonValueKind.String
                    ? permalinkElement.GetString()
                    : null;
            return new SocialPublisherPostSnapshotResult(true, true, message, permalink, null, null);
        }
        catch (JsonException)
        {
            return new SocialPublisherPostSnapshotResult(
                false,
                false,
                null,
                null,
                "facebook-invalid-response",
                "Facebook a renvoyé une réponse invalide.");
        }
    }

    private async Task<SocialPublisherOperationResult> SendMutationAsync(
        HttpMethod method,
        string externalPostId,
        IReadOnlyDictionary<string, string>? formValues,
        CancellationToken cancellationToken)
    {
        if (!this.settings.IsConfigured())
        {
            return new SocialPublisherOperationResult(
                false,
                false,
                "publisher-not-configured",
                "La Page Facebook n'est pas configurée.");
        }

        string endpoint = BuildObjectEndpoint(this.settings.ApiVersion, externalPostId);
        using HttpRequestMessage httpRequest = this.CreateAuthorizedRequest(method, endpoint);
        if (formValues is not null)
        {
            httpRequest.Content = new FormUrlEncodedContent(formValues);
        }

        HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
        using HttpResponseMessage response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return new SocialPublisherOperationResult(true, false, null, null);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound || IsMissingObject(responseBody))
        {
            return new SocialPublisherOperationResult(false, true, null, null);
        }

        SocialPublisherResult failure = ParseFailure(
            responseBody,
            ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture));
        return new SocialPublisherOperationResult(
            false,
            false,
            failure.FailureCode,
            failure.FailureMessage);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string endpoint)
    {
        HttpRequestMessage request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.settings.PageAccessToken);
        return request;
    }

    private static string BuildObjectEndpoint(string apiVersion, string externalPostId)
    {
        return $"{GraphApiBaseUrl}/{apiVersion}/{Uri.EscapeDataString(externalPostId)}";
    }

    private static bool IsMissingObject(string responseBody)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("error", out JsonElement errorElement)
                || errorElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            int code = errorElement.TryGetProperty("code", out JsonElement codeElement)
                && codeElement.TryGetInt32(out int parsedCode)
                    ? parsedCode
                    : 0;
            int subcode = errorElement.TryGetProperty("error_subcode", out JsonElement subcodeElement)
                && subcodeElement.TryGetInt32(out int parsedSubcode)
                    ? parsedSubcode
                    : 0;
            return code == 803 || (code == 100 && subcode == 33);
        }
        catch (JsonException)
        {
            return false;
        }
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

    private static bool HasMatchingPublishedMessage(
        JsonElement element,
        string expectedMessage,
        DateTimeOffset attemptedAtUtc)
    {
        string? id = TryGetString(element, "id");
        string? message = TryGetString(element, "message");
        string? createdAtValue = TryGetString(element, "created_time");
        return !string.IsNullOrWhiteSpace(id)
            && string.Equals(message, expectedMessage, StringComparison.Ordinal)
            && DateTimeOffset.TryParse(
                createdAtValue,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out DateTimeOffset createdAtUtc)
            && createdAtUtc >= attemptedAtUtc.AddMinutes(-2)
            && createdAtUtc <= attemptedAtUtc.AddMinutes(10);
    }

    private static bool TryGetNextCursor(
        JsonElement root,
        out bool hasNextPage,
        out string? afterCursor)
    {
        hasNextPage = false;
        afterCursor = null;
        if (!root.TryGetProperty("paging", out JsonElement pagingElement))
        {
            return true;
        }

        if (pagingElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        string? nextUrl = TryGetString(pagingElement, "next");
        if (string.IsNullOrWhiteSpace(nextUrl))
        {
            return true;
        }

        hasNextPage = true;
        if (!pagingElement.TryGetProperty("cursors", out JsonElement cursorsElement)
            || cursorsElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        afterCursor = TryGetString(cursorsElement, "after")?.Trim();
        return !string.IsNullOrWhiteSpace(afterCursor);
    }

    private static SocialPublisherLinkReconciliationResult BuildReconciliationResult(
        IReadOnlyCollection<(string Id, string? Url)> matches)
    {
        if (matches.Count == 0)
        {
            return new SocialPublisherLinkReconciliationResult(
                true,
                false,
                false,
                null,
                null,
                null,
                null);
        }

        (string id, string? url) = matches.Single();
        return new SocialPublisherLinkReconciliationResult(
            true,
            true,
            false,
            id,
            string.IsNullOrWhiteSpace(url) ? BuildExternalPostUrl(id) : url,
            null,
            null);
    }

    private static SocialPublisherLinkReconciliationResult AmbiguousReconciliation()
    {
        return new SocialPublisherLinkReconciliationResult(
            true,
            false,
            true,
            null,
            null,
            null,
            null);
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
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

    private static SocialPublisherLinkReconciliationResult ReconciliationFailure(string code, string message)
    {
        string safeMessage = message.Length <= MaximumStoredErrorLength
            ? message
            : message[..MaximumStoredErrorLength];
        return new SocialPublisherLinkReconciliationResult(
            false,
            false,
            false,
            null,
            null,
            code,
            safeMessage);
    }
}
