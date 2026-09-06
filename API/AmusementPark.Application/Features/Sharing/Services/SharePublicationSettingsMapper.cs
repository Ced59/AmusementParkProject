using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

internal static class SharePublicationSettingsMapper
{
    public static SharePublicationSettingsResult ToResult(SharePublication? publication)
    {
        bool isPublic = publication?.IsResolvable == true;
        return new SharePublicationSettingsResult(
            isPublic,
            isPublic ? publication!.ShareToken!.Value.Value : null,
            isPublic ? publication!.PublishedAtUtc : null);
    }
}
