using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class UpdateParkItemCommandHandler : ICommandHandler<UpdateParkItemCommand, ApplicationResult<ParkItem>>
{
    private readonly IParkItemRepository parkItemRepository;

    public UpdateParkItemCommandHandler(IParkItemRepository parkItemRepository)
    {
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<ParkItem>> HandleAsync(UpdateParkItemCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ParkItemId))
        {
            return ApplicationResult<ParkItem>.Failure(ApplicationErrors.Required(nameof(command.ParkItemId)));
        }

        if (command.ParkItem is null)
        {
            return ApplicationResult<ParkItem>.Failure(ApplicationErrors.Required(nameof(command.ParkItem)));
        }

        ParkItem? updated = await this.parkItemRepository.UpdateAsync(command.ParkItemId, command.ParkItem, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<ParkItem>.Failure(ApplicationErrors.EntityNotFound("ParkItem", command.ParkItemId));
        }

        return ApplicationResult<ParkItem>.Success(updated);
    }
}
