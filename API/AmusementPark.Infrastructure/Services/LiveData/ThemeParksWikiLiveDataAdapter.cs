using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Infrastructure.Services.LiveData;

public sealed class ThemeParksWikiLiveDataAdapter : ILiveDataProviderAdapter
{
    public const string HttpClientName = "themeparks-wiki-live";

    public const int MaximumResponseBytes = 2 * 1024 * 1024;

    public const int MaximumObservationCount = 10_000;

    public const int MaximumDiagnosticCount = 1_000;

    public const int MaximumQueueMemberCount = 32;

    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    private const string Version = "themeparks-wiki-rest-v1/1.14.0-adapter-1";
    private static readonly LiveDataSourceId ProviderSourceId = LiveDataSourceId.Parse("themeparks-wiki");

    private readonly IHttpClientFactory httpClientFactory;
    private readonly TimeSpan requestTimeout;
    private readonly TimeProvider timeProvider;

    public ThemeParksWikiLiveDataAdapter(IHttpClientFactory httpClientFactory)
        : this(httpClientFactory, TimeProvider.System, RequestTimeout)
    {
    }

    internal ThemeParksWikiLiveDataAdapter(
        IHttpClientFactory httpClientFactory,
        TimeProvider timeProvider,
        TimeSpan requestTimeout)
    {
        if (requestTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        }

        this.httpClientFactory = httpClientFactory;
        this.timeProvider = timeProvider;
        this.requestTimeout = requestTimeout;
    }

    public LiveDataSourceId SourceId => ProviderSourceId;

    public string AdapterVersion => Version;

    public async Task<LiveProviderReadResult> FetchLatestAsync(
        LiveProviderReadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        HttpClient httpClient = this.httpClientFactory.CreateClient(HttpClientName);
        string relativeUrl = $"v1/entity/{Uri.EscapeDataString(request.ExternalEntityId)}/live";
        using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        if (request.EntityTag is not null)
        {
            httpRequest.Headers.TryAddWithoutValidation("If-None-Match", request.EntityTag);
        }

        using CancellationTokenSource requestDeadline =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        requestDeadline.CancelAfter(this.requestTimeout);
        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                requestDeadline.Token);
            DateTime receivedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            string? entityTag = NormalizeResponseEntityTag(response.Headers.ETag?.ToString());

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                if (request.EntityTag is null)
                {
                    return new LiveProviderReadResult(
                        LiveProviderReadDisposition.Unavailable,
                        receivedAtUtc);
                }

                return new LiveProviderReadResult(
                    LiveProviderReadDisposition.NotModified,
                    receivedAtUtc,
                    entityTag: request.EntityTag);
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return new LiveProviderReadResult(
                    LiveProviderReadDisposition.RateLimited,
                    receivedAtUtc,
                    retryAfter: ResolveRetryAfter(response, receivedAtUtc));
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new LiveProviderReadResult(
                    LiveProviderReadDisposition.Unavailable,
                    receivedAtUtc);
            }

            if (response.Content.Headers.ContentLength > MaximumResponseBytes)
            {
                return new LiveProviderReadResult(
                    LiveProviderReadDisposition.ResponseTooLarge,
                    receivedAtUtc);
            }

            byte[]? payload = await ReadBoundedPayloadAsync(
                response.Content,
                requestDeadline.Token);
            if (payload is null)
            {
                return new LiveProviderReadResult(
                    LiveProviderReadDisposition.ResponseTooLarge,
                    receivedAtUtc);
            }

            string payloadSha256 = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
            JsonDocument providerDocument;
            try
            {
                providerDocument = JsonDocument.Parse(payload);
            }
            catch (JsonException)
            {
                return new LiveProviderReadResult(
                    LiveProviderReadDisposition.InvalidPayload,
                    receivedAtUtc,
                    payloadSha256: payloadSha256);
            }

            using (providerDocument)
            {
                JsonElement root = providerDocument.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || ThemeParksWikiLiveDataNormalizer.HasDuplicateProperties(root)
                    || !root.TryGetProperty("id", out JsonElement rootId)
                    || rootId.ValueKind != JsonValueKind.String
                    || !string.Equals(
                        rootId.GetString(),
                        request.ExternalEntityId,
                        StringComparison.Ordinal)
                    || !root.TryGetProperty("liveData", out JsonElement liveData)
                    || liveData.ValueKind != JsonValueKind.Array
                    || liveData.GetArrayLength() > MaximumObservationCount)
                {
                    return new LiveProviderReadResult(
                        LiveProviderReadDisposition.InvalidPayload,
                        receivedAtUtc,
                        payloadSha256: payloadSha256);
                }

                List<LiveProviderDiagnostic> diagnostics = new List<LiveProviderDiagnostic>();
                IReadOnlyCollection<ExternalLiveObservation> observations =
                    ThemeParksWikiLiveDataNormalizer.Normalize(liveData, diagnostics);
                return new LiveProviderReadResult(
                    LiveProviderReadDisposition.Success,
                    receivedAtUtc,
                    observations,
                    diagnostics,
                    entityTag,
                    payloadSha256: payloadSha256);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new LiveProviderReadResult(
                LiveProviderReadDisposition.Unavailable,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (HttpRequestException)
        {
            return new LiveProviderReadResult(
                LiveProviderReadDisposition.Unavailable,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (IOException)
        {
            return new LiveProviderReadResult(
                LiveProviderReadDisposition.Unavailable,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (InvalidDataException)
        {
            return new LiveProviderReadResult(
                LiveProviderReadDisposition.Unavailable,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
    }

    private static async Task<byte[]?> ReadBoundedPayloadAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken);
        using MemoryStream buffer = new MemoryStream();
        byte[] chunk = new byte[16 * 1024];
        int totalBytes = 0;
        while (true)
        {
            int bytesRead = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
            if (totalBytes > MaximumResponseBytes)
            {
                return null;
            }

            await buffer.WriteAsync(chunk.AsMemory(0, bytesRead), cancellationToken);
        }

        return buffer.ToArray();
    }

    private static TimeSpan? ResolveRetryAfter(HttpResponseMessage response, DateTime receivedAtUtc)
    {
        if (response.Headers.RetryAfter?.Delta is TimeSpan delta)
        {
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        if (response.Headers.RetryAfter?.Date is DateTimeOffset retryAt)
        {
            TimeSpan delay = retryAt.UtcDateTime - receivedAtUtc;
            return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }

        return null;
    }

    private static string? NormalizeResponseEntityTag(string? entityTag)
    {
        return LiveProviderEntityTag.TryNormalize(entityTag, out string? normalizedEntityTag)
            ? normalizedEntityTag
            : null;
    }
}
