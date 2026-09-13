using System.Collections.Concurrent;
using System.Threading.Channels;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Sharing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.WebAPI.OutputCaching;

/// <summary>
/// Regroupe les purges d'une même publication et les rejoue sans faire dépendre
/// l'écriture autoritative de la disponibilité du moteur SSR.
/// </summary>
public sealed class SharePublicationCacheInvalidationQueue
    : BackgroundService, ISharePublicationCacheInvalidationQueue
{
    private static readonly IReadOnlyCollection<string> PublicLanguages = new[]
    {
        "fr", "en", "de", "nl", "pl", "pt", "it", "es",
    };

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);
    private readonly ConcurrentDictionary<string, SharePublicationCacheInvalidationRequest> pending =
        new ConcurrentDictionary<string, SharePublicationCacheInvalidationRequest>(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> scheduledRetries =
        new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
    private readonly Channel<string> publicationIds = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });
    private readonly IShareSocialImageCacheInvalidator socialImageCacheInvalidator;
    private readonly ISsrPageCacheInvalidator ssrPageCacheInvalidator;
    private readonly ILogger<SharePublicationCacheInvalidationQueue> logger;

    public SharePublicationCacheInvalidationQueue(
        IShareSocialImageCacheInvalidator socialImageCacheInvalidator,
        ISsrPageCacheInvalidator ssrPageCacheInvalidator,
        ILogger<SharePublicationCacheInvalidationQueue> logger)
    {
        this.socialImageCacheInvalidator = socialImageCacheInvalidator;
        this.ssrPageCacheInvalidator = ssrPageCacheInvalidator;
        this.logger = logger;
    }

    public void Enqueue(SharePublicationCacheInvalidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.PublicationId)
            || request.ShareIds.Count == 0)
        {
            return;
        }

        string publicationId = request.PublicationId.Trim();
        SharePublicationCacheInvalidationRequest normalizedRequest = Normalize(request);
        this.pending.AddOrUpdate(
            publicationId,
            normalizedRequest,
            (_, current) => Merge(current, normalizedRequest));

        // Plusieurs notifications du même identifiant sont sans effet de bord : le dictionnaire
        // les fusionne. Toujours réveiller le lecteur évite de perdre une notification ajoutée
        // exactement pendant que le worker retire la précédente.
        this.publicationIds.Writer.TryWrite(publicationId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (string publicationId in this.publicationIds.Reader.ReadAllAsync(stoppingToken))
        {
            if (!this.pending.TryRemove(
                    publicationId,
                    out SharePublicationCacheInvalidationRequest? request))
            {
                continue;
            }

            bool succeeded = await this.TryInvalidateAsync(request, stoppingToken);
            if (succeeded)
            {
                continue;
            }

            this.pending.AddOrUpdate(
                publicationId,
                request,
                (_, current) => Merge(current, request));
            if (this.scheduledRetries.TryAdd(publicationId, 0))
            {
                _ = this.RequeueAfterDelayAsync(publicationId, stoppingToken);
            }
        }
    }

    internal async Task<bool> TryInvalidateAsync(
        SharePublicationCacheInvalidationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            this.socialImageCacheInvalidator.Invalidate(request.ShareIds);
            bool ssrSucceeded = await this.ssrPageCacheInvalidator.TryInvalidateAsync(
                BuildSsrRequest(request),
                cancellationToken);
            if (!ssrSucceeded)
            {
                this.logger.LogWarning(
                    "Share publication SSR cache invalidation was not confirmed for publication {PublicationId}.",
                    request.PublicationId);
            }

            return ssrSucceeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Share publication cache invalidation failed for publication {PublicationId}.",
                request.PublicationId);
            return false;
        }
    }

    internal static SsrPageCacheInvalidationRequest BuildSsrRequest(
        SharePublicationCacheInvalidationRequest request)
    {
        string routeSegment = request.PublicationType switch
        {
            SharePublicationType.PersonalRanking => "rankings/shared",
            SharePublicationType.VisitRecap => "passport/shared/visits",
            SharePublicationType.YearRecap => "passport/shared/years",
            SharePublicationType.PassportProfile => "passport/shared/profiles",
            _ => string.Empty,
        };
        if (routeSegment.Length == 0)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        IReadOnlyCollection<string> paths = request.ShareIds
            .SelectMany(shareId => PublicLanguages.Select(
                language => $"/{language}/{routeSegment}/{Uri.EscapeDataString(shareId)}"))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return new SsrPageCacheInvalidationRequest
        {
            All = false,
            Paths = paths,
            IncludeSeoDocuments = false,
            AllowStale = false,
            Refresh = false,
        };
    }

    private static SharePublicationCacheInvalidationRequest Normalize(
        SharePublicationCacheInvalidationRequest request)
    {
        return request with
        {
            PublicationId = request.PublicationId.Trim(),
            ShareIds = request.ShareIds
                .Where(static shareId => !string.IsNullOrWhiteSpace(shareId))
                .Select(static shareId => shareId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList(),
        };
    }

    private static SharePublicationCacheInvalidationRequest Merge(
        SharePublicationCacheInvalidationRequest current,
        SharePublicationCacheInvalidationRequest next)
    {
        return current with
        {
            PublicationType = next.PublicationType,
            ShareIds = current.ShareIds
                .Concat(next.ShareIds)
                .Where(static shareId => !string.IsNullOrWhiteSpace(shareId))
                .Select(static shareId => shareId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList(),
        };
    }

    private async Task RequeueAfterDelayAsync(
        string publicationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(RetryDelay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // L'arrêt du worker abandonne seulement la convergence best-effort en mémoire.
            this.scheduledRetries.TryRemove(publicationId, out _);
            return;
        }

        this.scheduledRetries.TryRemove(publicationId, out _);
        this.publicationIds.Writer.TryWrite(publicationId);
    }
}
