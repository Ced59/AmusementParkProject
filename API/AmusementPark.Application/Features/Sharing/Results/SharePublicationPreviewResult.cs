using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record SharePublicationPreviewResult(
    SharePublicationType PublicationType,
    long SourceVersion,
    int PolicySchemaVersion,
    ShareDatePrecision DatePrecision,
    IReadOnlyCollection<ShareContentField> IncludedFields,
    PersonalRankingSharePreviewResult? PersonalRanking,
    string ApprovalToken = "");
