using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Queries;

public sealed record PreviewSharePublicationQuery(
    string OwnerUserId,
    SharePublicationType PublicationType,
    string? SourceId,
    ShareDatePrecision DatePrecision,
    IReadOnlyCollection<ShareContentField> IncludedFields)
    : IQuery<ApplicationResult<SharePublicationPreviewResult>>;
