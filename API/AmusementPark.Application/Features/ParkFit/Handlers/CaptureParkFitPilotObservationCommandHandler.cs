using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class CaptureParkFitPilotObservationCommandHandler
    : ICommandHandler<CaptureParkFitPilotObservationCommand, ApplicationResult>
{
    private readonly IParkFitPilotMetricsRepository repository;
    private readonly TimeProvider timeProvider;

    public CaptureParkFitPilotObservationCommandHandler(
        IParkFitPilotMetricsRepository repository,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult> HandleAsync(
        CaptureParkFitPilotObservationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        ParkFitPilotObservation observation;
        try
        {
            observation = ParkFitPilotObservation.Create(
                command.EventKind,
                command.ResultBand,
                command.UnknownLevel,
                command.DurationBand,
                command.FailureKind,
                command.ComparisonSize,
                command.MethodVersion,
                command.QualityIssues);
        }
        catch (ArgumentException)
        {
            return ApplicationResult.Failure(
                ParkFitPilotApplicationErrors.InvalidObservation());
        }

        DateOnly dateUtc = DateOnly.FromDateTime(this.timeProvider.GetUtcNow().UtcDateTime);
        await this.repository.IncrementAsync(dateUtc, observation, cancellationToken);
        return ApplicationResult.Success();
    }
}
