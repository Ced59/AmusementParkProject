using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

public sealed class ChangeParkFitOperationalStatusCommandHandler
    : ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly IParkFitOperationalStatusRepository statusRepository;
    private readonly TimeProvider timeProvider;
    private readonly ParkFitDataQualityAssessor qualityAssessor;

    public ChangeParkFitOperationalStatusCommandHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkOpeningHoursRepository openingHoursRepository,
        IParkFitOperationalStatusRepository statusRepository,
        TimeProvider? timeProvider = null,
        ParkFitDataQualityAssessor? qualityAssessor = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.statusRepository = statusRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.qualityAssessor = qualityAssessor ?? new ParkFitDataQualityAssessor();
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
                and not ParkFitRecommendationState.Suspended
                and not ParkFitRecommendationState.NotActivated)
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
            cancellationToken) ?? ParkFitOperationalStatus.CreateNotActivated(parkId);
        if (status.Revision != command.ExpectedRevision)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.Conflict());
        }

        bool transitionAllowed = command.TargetState switch
        {
            ParkFitRecommendationState.Active =>
                status.State is ParkFitRecommendationState.NotActivated
                    or ParkFitRecommendationState.Suspended,
            ParkFitRecommendationState.Suspended =>
                status.State == ParkFitRecommendationState.Active,
            ParkFitRecommendationState.NotActivated =>
                status.State is ParkFitRecommendationState.Active
                    or ParkFitRecommendationState.Suspended,
            _ => false,
        };
        if (!transitionAllowed)
        {
            return ApplicationResult.Failure(ParkFitOperationsApplicationErrors.InvalidTransition());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        if (command.TargetState == ParkFitRecommendationState.Active)
        {
            Task<IReadOnlyCollection<ParkItem>> itemsTask =
                this.parkItemRepository.GetVisibleOpenAttractionsByParkIdsAsync(
                    new[] { parkId },
                    cancellationToken);
            Task<IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary>> summaryTask =
                this.openingHoursRepository.GetSummariesByParkIdsAsync(
                    new[] { parkId },
                    cancellationToken);
            await Task.WhenAll(itemsTask, summaryTask);
            IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary> summaries =
                await summaryTask;
            ParkFitDataQualityAssessment assessment = this.qualityAssessor.Assess(
                park,
                await itemsTask,
                summaries.GetValueOrDefault(parkId),
                nowUtc,
                ParkFitSearchLimits.MaximumVerificationAge);
            if (assessment.Status != ParkFitDataQualityStatus.EligibleForFitComparison)
            {
                return ApplicationResult.Failure(
                    ParkFitOperationsApplicationErrors.ActivationQualityRequired());
            }
        }

        DateTime decidedAtUtc = status.UpdatedAtUtc.HasValue && nowUtc < status.UpdatedAtUtc.Value
            ? status.UpdatedAtUtc.Value
            : nowUtc;
        try
        {
            if (command.TargetState == ParkFitRecommendationState.Suspended)
            {
                status.Suspend(command.ActorUserId, command.Reason, decidedAtUtc);
            }
            else if (command.TargetState == ParkFitRecommendationState.NotActivated)
            {
                status.Deactivate(command.ActorUserId, command.Reason, decidedAtUtc);
            }
            else if (status.State == ParkFitRecommendationState.Suspended)
            {
                status.RestoreRecommendations(command.ActorUserId, command.Reason, decidedAtUtc);
            }
            else
            {
                status.Activate(command.ActorUserId, command.Reason, decidedAtUtc);
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
