using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Queries;

public sealed record PreviewSharePublicationQuery(
    string OwnerUserId,
    SharePublicationType PublicationType,
    string? SourceId,
    ShareDatePrecision DatePrecision,
    IReadOnlyCollection<ShareContentField> IncludedFields,
    VisitRecapShareInput? VisitRecap = null,
    YearRecapShareInput? YearRecap = null,
    PassportProfileShareInput? PassportProfile = null)
    : IQuery<ApplicationResult<SharePublicationPreviewResult>>;
