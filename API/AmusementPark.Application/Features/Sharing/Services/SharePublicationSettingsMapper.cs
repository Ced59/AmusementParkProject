using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

internal static class SharePublicationSettingsMapper
{
    public static SharePublicationSettingsResult ToResult(
        SharePublication? publication,
        bool isSourceCurrent = true)
    {
        bool isModerationSuspended = publication?.IsModerationSuspended == true;
        bool hasOwnerPublicationControls = publication?.Status == SharePublicationStatus.Published
            && (isSourceCurrent || isModerationSuspended);
        return new SharePublicationSettingsResult(
            hasOwnerPublicationControls,
            hasOwnerPublicationControls ? publication!.ShareToken!.Value.Value : null,
            hasOwnerPublicationControls ? publication!.PublishedAtUtc : null,
            publication?.ContentPolicy.SchemaVersion,
            publication?.ContentPolicy.DatePrecision,
            publication?.ContentPolicy.IncludedFields ?? Array.Empty<ShareContentField>(),
            publication?.Visibility,
            publication?.Id.Value,
            publication?.PublicationVersion,
            isModerationSuspended);
    }
}
