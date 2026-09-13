using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ShareModerationPublicationTargetExecutor
{
    private readonly ISharePublicationRepository publicationRepository;
    private readonly SharePublicationCacheInvalidationScheduler invalidationScheduler;
    private readonly TimeProvider timeProvider;

    public ShareModerationPublicationTargetExecutor(
        ISharePublicationRepository publicationRepository,
        SharePublicationCacheInvalidationScheduler invalidationScheduler,
        TimeProvider? timeProvider = null)
    {
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.invalidationScheduler = invalidationScheduler
            ?? throw new ArgumentNullException(nameof(invalidationScheduler));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ShareModerationDecisionExecutionOutcome> SuspendAsync(
        ShareModerationReport report,
        CancellationToken cancellationToken)
    {
        SharePublication? publication = await this.GetTargetAsync(report, cancellationToken);
        if (publication is null)
        {
            return ShareModerationDecisionExecutionOutcome.TargetNotFound;
        }

        if (publication.ModerationSuspensionReportId == report.Id)
        {
            await this.ScheduleInvalidationAsync(publication, cancellationToken);
            return ShareModerationDecisionExecutionOutcome.Succeeded;
        }

        if (publication.IsModerationSuspended)
        {
            return ShareModerationDecisionExecutionOutcome.RetryableConflict;
        }

        long expectedVersion = publication.Version;
        string? shareId = publication.ShareToken?.Value;
        try
        {
            publication.SuspendByModeration(
                report.Id,
                this.NextTimestamp(publication.UpdatedAtUtc));
        }
        catch (SharePublicationValidationException)
        {
            return ShareModerationDecisionExecutionOutcome.InvalidTransition;
        }

        SharePublicationWriteOutcome outcome = await this.publicationRepository.ReplaceAsync(
            publication,
            expectedVersion,
            cancellationToken);
        if (outcome != SharePublicationWriteOutcome.Success)
        {
            return ShareModerationDecisionExecutionOutcome.RetryableConflict;
        }

        await this.invalidationScheduler.ScheduleAsync(
            publication,
            publication.Version,
            cancellationToken,
            shareId);
        return ShareModerationDecisionExecutionOutcome.Succeeded;
    }

    public async Task<ShareModerationDecisionExecutionOutcome> RestoreAsync(
        ShareModerationReport report,
        CancellationToken cancellationToken)
    {
        SharePublication? publication = await this.GetTargetAsync(report, cancellationToken);
        if (publication is null)
        {
            return ShareModerationDecisionExecutionOutcome.TargetNotFound;
        }

        if (!publication.IsModerationSuspended
            || publication.ModerationSuspensionReportId != report.Id)
        {
            await this.ScheduleInvalidationAsync(publication, cancellationToken);
            return ShareModerationDecisionExecutionOutcome.Succeeded;
        }

        long expectedVersion = publication.Version;
        string? shareId = publication.ShareToken?.Value;
        try
        {
            publication.RestoreAfterModeration(
                report.Id,
                this.NextTimestamp(publication.UpdatedAtUtc));
        }
        catch (SharePublicationValidationException)
        {
            return ShareModerationDecisionExecutionOutcome.InvalidTransition;
        }

        SharePublicationWriteOutcome outcome = await this.publicationRepository.ReplaceAsync(
            publication,
            expectedVersion,
            cancellationToken);
        if (outcome != SharePublicationWriteOutcome.Success)
        {
            return ShareModerationDecisionExecutionOutcome.RetryableConflict;
        }

        await this.invalidationScheduler.ScheduleAsync(
            publication,
            publication.Version,
            cancellationToken,
            shareId);
        return ShareModerationDecisionExecutionOutcome.Succeeded;
    }

    private async Task<SharePublication?> GetTargetAsync(
        ShareModerationReport report,
        CancellationToken cancellationToken)
    {
        if (!TryMapPublicationType(report.TargetType, out SharePublicationType publicationType)
            || !SharePublicationId.TryParse(
                report.TargetRecordId,
                out SharePublicationId publicationId))
        {
            return null;
        }

        SharePublication? publication = await this.publicationRepository.GetByIdAsync(
            publicationId,
            cancellationToken);
        return publication?.Type == publicationType ? publication : null;
    }

    private DateTime NextTimestamp(DateTime updatedAtUtc)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        return nowUtc < updatedAtUtc ? updatedAtUtc : nowUtc;
    }

    private Task ScheduleInvalidationAsync(
        SharePublication publication,
        CancellationToken cancellationToken)
    {
        return this.invalidationScheduler.ScheduleAsync(
            publication,
            publication.Version,
            cancellationToken,
            publication.ShareToken?.Value);
    }

    private static bool TryMapPublicationType(
        ShareModerationTargetType targetType,
        out SharePublicationType publicationType)
    {
        publicationType = targetType switch
        {
            ShareModerationTargetType.VisitRecap => SharePublicationType.VisitRecap,
            ShareModerationTargetType.YearRecap => SharePublicationType.YearRecap,
            ShareModerationTargetType.PassportProfile => SharePublicationType.PassportProfile,
            ShareModerationTargetType.PersonalRanking => SharePublicationType.PersonalRanking,
            _ => default,
        };
        return targetType != ShareModerationTargetType.ProfileComparison
            && Enum.IsDefined(publicationType);
    }
}
