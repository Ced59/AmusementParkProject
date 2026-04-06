using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class CreateParkItemCommandHandler : ICommandHandler<CreateParkItemCommand, ApplicationResult<ParkItem>>
{
    private readonly IParkItemRepository parkItemRepository;

    public CreateParkItemCommandHandler(IParkItemRepository parkItemRepository)
    {
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<ParkItem>> HandleAsync(CreateParkItemCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ParkItem is null)
        {
            return ApplicationResult<ParkItem>.Failure(ApplicationErrors.Required(nameof(command.ParkItem)));
        }

        ParkItem created = await this.parkItemRepository.CreateAsync(command.ParkItem, cancellationToken);
        return ApplicationResult<ParkItem>.Success(created);
    }
}
