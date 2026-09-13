using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record SharePublicationPreviewResult(
    SharePublicationType PublicationType,
    long SourceVersion,
    int PolicySchemaVersion,
    ShareDatePrecision DatePrecision,
    IReadOnlyCollection<ShareContentField> IncludedFields,
    PersonalRankingSharePreviewResult? PersonalRanking,
    string ApprovalToken = "",
    VisitRecapSharePreviewResult? VisitRecap = null,
    string ContentFingerprint = "",
    YearRecapSharePreviewResult? YearRecap = null,
    PassportProfileSharePreviewResult? PassportProfile = null)
{
    internal IReadOnlyCollection<PassportProfileShareSelectedParkSnapshot>
        PassportProfileSelectedParks { get; init; } =
            Array.Empty<PassportProfileShareSelectedParkSnapshot>();
}
