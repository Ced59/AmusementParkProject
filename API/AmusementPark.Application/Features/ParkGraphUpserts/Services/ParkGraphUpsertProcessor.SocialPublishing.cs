using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task PublishNewlyVisibleParkAsync(
        Park park,
        bool wasPubliclyDiscoverable,
        string? requestedByUserId,
        ParkGraphUpsertResult result,
        CancellationToken cancellationToken)
    {
        if (this.socialPublicationService is null
            || wasPubliclyDiscoverable
            || !park.IsPubliclyDiscoverable())
        {
            return;
        }

        try
        {
            SocialPublication? publication = await this.socialPublicationService.PublishParkAnnouncementAsync(
                park,
                requestedByUserId,
                cancellationToken);

            if (publication?.Status == SocialPublicationStatus.Failed)
            {
                result.Warnings.Add("Le parc est maintenant public, mais son annonce Facebook n'a pas pu être envoyée. Elle peut être relancée depuis l'administration des publications sociales.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            result.Warnings.Add("Le parc est maintenant public, mais la préparation de son annonce Facebook a échoué. Vérifier l'administration des publications sociales.");
        }
    }
}
