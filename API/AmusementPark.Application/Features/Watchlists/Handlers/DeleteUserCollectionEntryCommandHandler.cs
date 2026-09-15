using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class DeleteUserCollectionEntryCommandHandler : ICommandHandler<
    DeleteUserCollectionEntryCommand,
    ApplicationResult>
{
    private readonly UserCollectionLifecycleService service;

    public DeleteUserCollectionEntryCommandHandler(UserCollectionLifecycleService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult> HandleAsync(
        DeleteUserCollectionEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.DeleteAsync(command.UserId, command.Target, cancellationToken);
    }
}
