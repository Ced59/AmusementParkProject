using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class SetTripItemPreferenceCommandHandler
    : ICommandHandler<SetTripItemPreferenceCommand, ApplicationResult<TripPreferenceBoardResult>>
{
    private readonly TripPreferenceService service;

    public SetTripItemPreferenceCommandHandler(TripPreferenceService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripPreferenceBoardResult>> HandleAsync(
        SetTripItemPreferenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.SetAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedPlanVersion,
            new[] { command.Preference },
            cancellationToken);
    }
}
