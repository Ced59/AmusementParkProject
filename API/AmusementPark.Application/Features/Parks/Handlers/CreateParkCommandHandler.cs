using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de création d'un parc.
/// </summary>
public sealed class CreateParkCommandHandler : ICommandHandler<CreateParkCommand, ApplicationResult<Park>>
{
    private readonly IParkRepository parkRepository;

    public CreateParkCommandHandler(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<Park>> HandleAsync(CreateParkCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Park is null)
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.Required(nameof(command.Park)));
        }

        Park created = await this.parkRepository.CreateAsync(command.Park, cancellationToken);
        return ApplicationResult<Park>.Success(created);
    }
}
