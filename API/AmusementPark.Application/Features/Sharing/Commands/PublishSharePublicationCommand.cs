using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;
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
    string ApprovalToken)
    : ICommand<ApplicationResult<SharePublicationSettingsResult>>;
