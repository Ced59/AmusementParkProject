using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Commands;

public sealed record SetSharePublicationVisibilityCommand(
    string UserId,
    SharePublicationType PublicationType,
    string? SourceId,
    bool IsPublic)
    : ICommand<ApplicationResult<SharePublicationSettingsResult>>;
