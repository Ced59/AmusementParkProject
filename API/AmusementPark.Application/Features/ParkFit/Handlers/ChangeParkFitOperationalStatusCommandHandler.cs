using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class ChangeParkFitOperationalStatusCommandHandler
    : ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkFitOperationalStatusRepository statusRepository;
    private readonly TimeProvider timeProvider;

    public ChangeParkFitOperationalStatusCommandHandler(
        IParkRepository parkRepository,
        IParkFitOperationalStatusRepository statusRepository,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.statusRepository = statusRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult> HandleAsync(
        ChangeParkFitOperationalStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        string parkId = command.ParkId?.Trim() ?? string.Empty;
        if (parkId.Length == 0
            || string.IsNullOrWhiteSpace(command.ActorUserId)
            || string.IsNullOrWhiteSpace(command.Reason)
            || command.ExpectedRevision < 0
            || command.TargetState is not ParkFitRecommendationState.Active
                and not ParkFitRecommendationState.Suspended)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.InvalidReport());
        }

        Park? park = await this.parkRepository.GetByIdAsync(
            parkId,
            includeHidden: true,
            cancellationToken);
        if (park is null)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.ParkNotFound());
        }

        ParkFitOperationalStatus status = await this.statusRepository.GetAsync(
            parkId,
            cancellationToken) ?? ParkFitOperationalStatus.CreateActive(parkId);
        if (status.Revision != command.ExpectedRevision)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.Conflict());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        DateTime decidedAtUtc = status.UpdatedAtUtc.HasValue && nowUtc < status.UpdatedAtUtc.Value
            ? status.UpdatedAtUtc.Value
            : nowUtc;
        try
        {
            if (command.TargetState == ParkFitRecommendationState.Suspended)
            {
                status.Suspend(command.ActorUserId, command.Reason, decidedAtUtc);
            }
            else
            {
                status.RestoreRecommendations(command.ActorUserId, command.Reason, decidedAtUtc);
            }
        }
        catch (ArgumentException)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.InvalidReport());
        }
        catch (InvalidOperationException)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.InvalidTransition());
        }

        ParkFitOperationalStatusWriteOutcome outcome = await this.statusRepository.ReplaceAsync(
            status,
            command.ExpectedRevision,
            cancellationToken);
        return outcome == ParkFitOperationalStatusWriteOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(ParkFitOperationsApplicationErrors.Conflict());
    }
}
