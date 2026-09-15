using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class AddUserCollectionEntryCommandHandler : ICommandHandler<
    AddUserCollectionEntryCommand,
    ApplicationResult<UserCollectionEntryResult>>
{
    private readonly UserCollectionLifecycleService service;

    public AddUserCollectionEntryCommandHandler(UserCollectionLifecycleService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<UserCollectionEntryResult>> HandleAsync(
        AddUserCollectionEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.AddAsync(command.UserId, command.Target, cancellationToken);
    }
}
