using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class DeleteParkItemCommandHandler : ICommandHandler<DeleteParkItemCommand, ApplicationResult>
{
    private readonly IParkItemRepository parkItemRepository;

    public DeleteParkItemCommandHandler(IParkItemRepository parkItemRepository)
    {
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult> HandleAsync(DeleteParkItemCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ParkItemId))
        {
            return ApplicationResult.Failure(ApplicationErrors.Required(nameof(command.ParkItemId)));
        }

        bool deleted = await this.parkItemRepository.DeleteAsync(command.ParkItemId, cancellationToken);
        if (!deleted)
        {
            return ApplicationResult.Failure(ApplicationErrors.EntityNotFound("ParkItem", command.ParkItemId));
        }

        return ApplicationResult.Success();
    }
}
