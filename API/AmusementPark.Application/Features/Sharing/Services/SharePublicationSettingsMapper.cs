using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

internal static class SharePublicationSettingsMapper
{
    public static SharePublicationSettingsResult ToResult(
        SharePublication? publication,
        bool isSourceCurrent = true)
    {
        bool isPublic = publication?.IsResolvable == true && isSourceCurrent;
        return new SharePublicationSettingsResult(
            isPublic,
            isPublic ? publication!.ShareToken!.Value.Value : null,
            isPublic ? publication!.PublishedAtUtc : null,
            publication?.ContentPolicy.SchemaVersion,
            publication?.ContentPolicy.DatePrecision,
            publication?.ContentPolicy.IncludedFields ?? Array.Empty<ShareContentField>());
    }
}
