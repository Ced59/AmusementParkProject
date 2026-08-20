using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Handlers;

internal static class PublicImageSeoUpdateNotification
{
    public static async Task NotifyAsync(
        IPublicSeoUpdateNotifier? notifier,
        IEnumerable<Image?> previousImages,
        IEnumerable<Image?> currentImages,
        CancellationToken cancellationToken)
    {
        if (notifier is null)
        {
            return;
        }

        IReadOnlyCollection<PublicSeoImageSnapshot> previousSnapshots = PublicSeoImageSnapshot.FromImages(previousImages);
        IReadOnlyCollection<PublicSeoImageSnapshot> currentSnapshots = PublicSeoImageSnapshot.FromImages(currentImages);
        if (previousSnapshots.Count == 0 && currentSnapshots.Count == 0)
        {
            return;
        }

        await notifier.NotifyAsync(
            new PublicSeoUpdate
            {
                PreviousImages = previousSnapshots,
                CurrentImages = currentSnapshots,
            },
            cancellationToken);
    }
}
