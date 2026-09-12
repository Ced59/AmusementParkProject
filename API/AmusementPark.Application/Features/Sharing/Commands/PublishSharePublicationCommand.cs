using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Commands;

public sealed record PublishSharePublicationCommand(
    string UserId,
    SharePublicationType PublicationType,
    string? SourceId,
    long ApprovedSourceVersion,
    int ApprovedPolicySchemaVersion,
    ShareDatePrecision ApprovedDatePrecision,
    IReadOnlyCollection<ShareContentField> ApprovedIncludedFields,
    string ApprovalToken,
    VisitRecapShareInput? VisitRecap = null,
    YearRecapShareInput? YearRecap = null,
    PassportProfileShareInput? PassportProfile = null)
    : ICommand<ApplicationResult<SharePublicationSettingsResult>>;
