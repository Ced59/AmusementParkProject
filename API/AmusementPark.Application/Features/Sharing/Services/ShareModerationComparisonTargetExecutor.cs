using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ShareModerationComparisonTargetExecutor
{
    private readonly IProfileComparisonRepository comparisonRepository;
    private readonly TimeProvider timeProvider;

    public ShareModerationComparisonTargetExecutor(
        IProfileComparisonRepository comparisonRepository,
        TimeProvider? timeProvider = null)
    {
        this.comparisonRepository = comparisonRepository
            ?? throw new ArgumentNullException(nameof(comparisonRepository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ShareModerationDecisionExecutionOutcome> SuspendAsync(
        ShareModerationReport report,
        CancellationToken cancellationToken)
    {
        ProfileComparison? comparison = await this.GetTargetAsync(report, cancellationToken);
        if (comparison is null || !comparison.IsActive)
        {
            return ShareModerationDecisionExecutionOutcome.TargetNotFound;
        }

        if (comparison.ModerationSuspensionReportId == report.Id)
        {
            return ShareModerationDecisionExecutionOutcome.Succeeded;
        }

        if (comparison.IsModerationSuspended)
        {
            return ShareModerationDecisionExecutionOutcome.RetryableConflict;
        }

        long expectedVersion = comparison.Version;
        try
        {
            comparison.SuspendByModeration(
                report.Id,
                this.NextTimestamp(comparison.UpdatedAtUtc));
        }
        catch (ProfileComparisonValidationException)
        {
            return ShareModerationDecisionExecutionOutcome.InvalidTransition;
        }

        ProfileComparisonWriteOutcome outcome = await this.comparisonRepository.ReplaceAsync(
            comparison,
            expectedVersion,
            cancellationToken);
        return outcome == ProfileComparisonWriteOutcome.Success
            ? ShareModerationDecisionExecutionOutcome.Succeeded
            : ShareModerationDecisionExecutionOutcome.RetryableConflict;
    }

    public async Task<ShareModerationDecisionExecutionOutcome> RestoreAsync(
        ShareModerationReport report,
        CancellationToken cancellationToken)
    {
        ProfileComparison? comparison = await this.GetTargetAsync(report, cancellationToken);
        if (comparison is null)
        {
            return ShareModerationDecisionExecutionOutcome.TargetNotFound;
        }

        if (!comparison.IsModerationSuspended
            || comparison.ModerationSuspensionReportId != report.Id)
        {
            return ShareModerationDecisionExecutionOutcome.Succeeded;
        }

        long expectedVersion = comparison.Version;
        try
        {
            comparison.RestoreAfterModeration(
                report.Id,
                this.NextTimestamp(comparison.UpdatedAtUtc));
        }
        catch (ProfileComparisonValidationException)
        {
            return ShareModerationDecisionExecutionOutcome.InvalidTransition;
        }

        ProfileComparisonWriteOutcome outcome = await this.comparisonRepository.ReplaceAsync(
            comparison,
            expectedVersion,
            cancellationToken);
        return outcome == ProfileComparisonWriteOutcome.Success
            ? ShareModerationDecisionExecutionOutcome.Succeeded
            : ShareModerationDecisionExecutionOutcome.RetryableConflict;
    }

    private async Task<ProfileComparison?> GetTargetAsync(
        ShareModerationReport report,
        CancellationToken cancellationToken)
    {
        if (!ProfileComparisonId.TryParse(
                report.TargetRecordId,
                out ProfileComparisonId comparisonId))
        {
            return null;
        }

        return await this.comparisonRepository.GetByIdAsync(
            comparisonId,
            cancellationToken);
    }

    private DateTime NextTimestamp(DateTime updatedAtUtc)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        return nowUtc < updatedAtUtc ? updatedAtUtc : nowUtc;
    }
}
