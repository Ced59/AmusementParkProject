using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Sharing;
using Microsoft.Extensions.Logging;

namespace AmusementPark.WebAPI.OutputCaching;

public sealed class SharePublicationCacheInvalidationExecutor
    : ISharePublicationCacheInvalidationExecutor
{
    private static readonly IReadOnlyCollection<string> PublicLanguages = new[]
    {
        "fr", "en", "de", "nl", "pl", "pt", "it", "es",
    };

    private readonly IShareSocialImageCacheInvalidator socialImageCacheInvalidator;
    private readonly ISsrPageCacheInvalidator ssrPageCacheInvalidator;
    private readonly ILogger<SharePublicationCacheInvalidationExecutor> logger;

    public SharePublicationCacheInvalidationExecutor(
        IShareSocialImageCacheInvalidator socialImageCacheInvalidator,
        ISsrPageCacheInvalidator ssrPageCacheInvalidator,
        ILogger<SharePublicationCacheInvalidationExecutor> logger)
    {
        this.socialImageCacheInvalidator = socialImageCacheInvalidator;
        this.ssrPageCacheInvalidator = ssrPageCacheInvalidator;
        this.logger = logger;
    }

    public async Task<bool> TryInvalidateAsync(
        SharePublicationCacheInvalidationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
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
            SharePublicationType.ProfileComparison => "passport/shared/comparisons",
            _ => string.Empty,
        };
        if (routeSegment.Length == 0)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        IReadOnlyCollection<string> paths = request.ShareIds
            .Where(static shareId => !string.IsNullOrWhiteSpace(shareId))
            .Select(static shareId => shareId.Trim())
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
}
