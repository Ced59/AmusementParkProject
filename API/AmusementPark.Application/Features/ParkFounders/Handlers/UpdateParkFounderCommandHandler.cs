using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFounders.Commands;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFounders.Handlers;

/// <summary>
/// Handler de mise à jour d'un park founder.
/// </summary>
public sealed class UpdateParkFounderCommandHandler : ICommandHandler<UpdateParkFounderCommand, ApplicationResult<ParkFounder>>
{
    private readonly IParkFounderRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="UpdateParkFounderCommandHandler"/>.
    /// </summary>
    public UpdateParkFounderCommandHandler(IParkFounderRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<ParkFounder>> HandleAsync(UpdateParkFounderCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            return ApplicationResult<ParkFounder>.Failure(ApplicationErrors.Required(nameof(command.Id)));
        }

        if (command.ParkFounder is null)
        {
            return ApplicationResult<ParkFounder>.Failure(ApplicationErrors.Required(nameof(command.ParkFounder)));
        }

        ParkFounder? updated = await this.repository.UpdateAsync(command.Id, command.ParkFounder, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<ParkFounder>.Failure(ApplicationErrors.EntityNotFound("ParkFounder", command.Id));
        }

        return ApplicationResult<ParkFounder>.Success(updated);
    }
}
