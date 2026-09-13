using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Application.Features.Sharing.Commands;

public sealed record RotateShareIdCommand(string UserId, string PublicationId)
    : ICommand<ApplicationResult<SharePublicationSettingsResult>>;
