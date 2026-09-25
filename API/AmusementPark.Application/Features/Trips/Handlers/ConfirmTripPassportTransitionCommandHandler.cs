using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class ConfirmTripPassportTransitionCommandHandler
    : ICommandHandler<ConfirmTripPassportTransitionCommand,
        ApplicationResult<ConfirmTripPassportTransitionResult>>
{
    private readonly TripPassportTransitionConfirmer confirmer;

    public ConfirmTripPassportTransitionCommandHandler(TripPassportTransitionConfirmer confirmer)
    {
        this.confirmer = confirmer ?? throw new ArgumentNullException(nameof(confirmer));
    }

    public Task<ApplicationResult<ConfirmTripPassportTransitionResult>> HandleAsync(
        ConfirmTripPassportTransitionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.confirmer.ConfirmAsync(
            command.UserId,
            command.TripPlanId,
            command.LocalDate,
            command.ParkItemIds,
            cancellationToken);
    }
}
