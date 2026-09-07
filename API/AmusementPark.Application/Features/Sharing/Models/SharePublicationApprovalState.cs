using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public readonly record struct SharePublicationApprovalState(
    string? PublicationId,
    long? PersistenceVersion)
{
    public static SharePublicationApprovalState From(SharePublication? publication)
    {
        return publication is null
            ? new SharePublicationApprovalState(null, null)
            : new SharePublicationApprovalState(
                publication.Id.Value,
                publication.Version);
    }

    public bool Matches(SharePublication? publication)
    {
        return this == From(publication);
    }
}
