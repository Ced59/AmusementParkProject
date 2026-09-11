using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record YearRecapShareSnapshot(
    SharePublicationId PublicationId,
    long PublicationVersion,
    long PublicationStateVersion,
    long SourceVersion,
    int PolicySchemaVersion,
    ShareDatePrecision DatePrecision,
    IReadOnlyCollection<ShareContentField> IncludedFields,
    string ContentFingerprint,
    YearRecapSharePreviewResult Content,
    DateTime CreatedAtUtc);
