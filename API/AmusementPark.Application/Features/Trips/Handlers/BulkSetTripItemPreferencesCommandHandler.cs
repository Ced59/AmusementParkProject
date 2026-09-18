using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class BulkSetTripItemPreferencesCommandHandler
    : ICommandHandler<BulkSetTripItemPreferencesCommand, ApplicationResult<TripPreferenceBoardResult>>
{
    private readonly TripPreferenceService service;

    public BulkSetTripItemPreferencesCommandHandler(TripPreferenceService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripPreferenceBoardResult>> HandleAsync(
        BulkSetTripItemPreferencesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.SetAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedPlanVersion,
            command.Preferences,
            cancellationToken);
    }
}
