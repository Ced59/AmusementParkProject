using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class SetTripItemDecisionCommandHandler
    : ICommandHandler<SetTripItemDecisionCommand, ApplicationResult<TripPreferenceSummaryResult>>
{
    private readonly TripPreferenceSummaryService service;

    public SetTripItemDecisionCommandHandler(TripPreferenceSummaryService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<TripPreferenceSummaryResult>> HandleAsync(
        SetTripItemDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.SetDecisionAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedPlanVersion,
            command.Decision,
            cancellationToken);
    }
}
