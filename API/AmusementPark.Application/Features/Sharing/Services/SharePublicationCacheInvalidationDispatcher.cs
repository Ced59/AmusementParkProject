using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class SharePublicationCacheInvalidationDispatcher
{
    public static void Enqueue(
        ISharePublicationCacheInvalidationQueue? queue,
        SharePublication publication,
        params string?[] shareIds)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (queue is null)
        {
            return;
        }

        IReadOnlyCollection<string> normalizedShareIds = shareIds
            .Where(static shareId => !string.IsNullOrWhiteSpace(shareId))
            .Select(static shareId => shareId!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalizedShareIds.Count == 0)
        {
            return;
        }

        try
        {
            queue.Enqueue(new SharePublicationCacheInvalidationRequest(
                publication.Id.Value,
                publication.Type,
                normalizedShareIds));
        }
        catch
        {
            // La publication persistée fait autorité. Toute lecture publique revalide son jeton,
            // même lorsque la convergence best-effort d'un cache dérivé est momentanément indisponible.
        }
    }
}
