using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripNotificationService
{
    private readonly ITripPlanRepository plans;
    private readonly ITripAuditReader audit;
    private readonly ITripNotificationSubscriptionRepository subscriptions;
    private readonly TimeProvider timeProvider;

    public TripNotificationService(
        ITripPlanRepository plans,
        ITripAuditReader audit,
        ITripNotificationSubscriptionRepository subscriptions,
        TimeProvider? timeProvider = null)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.audit = audit ?? throw new ArgumentNullException(nameof(audit));
        this.subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<TripNotificationStateResult>> GetAsync(
        string userId,
        string tripPlanId,
        CancellationToken cancellationToken)
    {
        TripNotificationAccess? access = await this.ResolveAccessAsync(
            userId,
            tripPlanId,
            cancellationToken);
        return access is null
            ? NotFound()
            : ApplicationResult<TripNotificationStateResult>.Success(
                await this.BuildStateAsync(access, cancellationToken));
    }

    public async Task<ApplicationResult<TripNotificationStateResult>> SetEnabledAsync(
        string userId,
        string tripPlanId,
        bool enabled,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (expectedVersion < 0)
        {
            return Invalid();
        }

        TripNotificationAccess? access = await this.ResolveAccessAsync(
            userId,
            tripPlanId,
            cancellationToken);
        if (access is null)
        {
            return NotFound();
        }

        TripNotificationSubscription? current = await this.subscriptions.GetAsync(
            access.Trip.Id,
            access.UserId,
            cancellationToken);
        if (current is not null && current.MemberId != access.Member.Id)
        {
            if (!await this.subscriptions.DeleteIfCurrentAsync(
                    current,
                    CancellationToken.None))
            {
                TripNotificationSubscription? concurrent =
                    await this.subscriptions.GetAsync(
                        access.Trip.Id,
                        access.UserId,
                        CancellationToken.None);
                return Changed(concurrent?.Version);
            }

            current = null;
        }
        if (current is null)
        {
            if (expectedVersion != 0)
            {
                return Changed(0);
            }

            if (!enabled)
            {
                return ApplicationResult<TripNotificationStateResult>.Success(
                    new TripNotificationStateResult(false, 0, 0, false));
            }

            long latestSequence = await this.audit.GetLatestSequenceAsync(
                access.Trip.Id,
                cancellationToken);
            current = TripNotificationSubscription.CreateEnabled(
                access.Trip.Id,
                access.Member.Id,
                access.UserId,
                latestSequence,
                this.timeProvider.GetUtcNow().UtcDateTime);
            if (!await this.subscriptions.CreateAsync(current, cancellationToken))
            {
                TripNotificationSubscription? concurrent = await this.subscriptions.GetAsync(
                    access.Trip.Id,
                    access.UserId,
                    cancellationToken);
                return Changed(concurrent?.Version);
            }

            if (!await this.IsStillAccessibleAsync(access, CancellationToken.None))
            {
                _ = await this.subscriptions.DeleteIfCurrentAsync(
                    current,
                    CancellationToken.None);
                return NotFound();
            }

            return ApplicationResult<TripNotificationStateResult>.Success(
                await this.BuildStateAsync(access, current, cancellationToken));
        }

        if (current.Version != expectedVersion)
        {
            return Changed(current.Version);
        }

        if (current.IsEnabled == enabled)
        {
            return ApplicationResult<TripNotificationStateResult>.Success(
                await this.BuildStateAsync(access, current, cancellationToken));
        }

        long sequence = await this.audit.GetLatestSequenceAsync(access.Trip.Id, cancellationToken);
        current.SetEnabled(enabled, sequence, this.timeProvider.GetUtcNow().UtcDateTime);
        if (!await this.subscriptions.ReplaceAsync(current, expectedVersion, cancellationToken))
        {
            TripNotificationSubscription? concurrent = await this.subscriptions.GetAsync(
                access.Trip.Id,
                access.UserId,
                cancellationToken);
            return Changed(concurrent?.Version);
        }

        return ApplicationResult<TripNotificationStateResult>.Success(
            await this.BuildStateAsync(access, current, cancellationToken));
    }

    public async Task<ApplicationResult<TripNotificationStateResult>> MarkReadAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (expectedVersion < 1)
        {
            return Invalid();
        }

        TripNotificationAccess? access = await this.ResolveAccessAsync(
            userId,
            tripPlanId,
            cancellationToken);
        if (access is null)
        {
            return NotFound();
        }

        TripNotificationSubscription? current = await this.subscriptions.GetAsync(
            access.Trip.Id,
            access.UserId,
            cancellationToken);
        if (current is null
            || current.MemberId != access.Member.Id
            || !current.IsEnabled)
        {
            return ApplicationResult<TripNotificationStateResult>.Failure(
                TripPlanApplicationErrors.NotificationsDisabled());
        }

        if (current.Version != expectedVersion)
        {
            return Changed(current.Version);
        }

        long latestSequence = await this.audit.GetLatestSequenceAsync(
            access.Trip.Id,
            cancellationToken);
        long previousVersion = current.Version;
        current.MarkSeenThrough(latestSequence, this.timeProvider.GetUtcNow().UtcDateTime);
        if (current.Version != previousVersion
            && !await this.subscriptions.ReplaceAsync(current, expectedVersion, cancellationToken))
        {
            TripNotificationSubscription? concurrent = await this.subscriptions.GetAsync(
                access.Trip.Id,
                access.UserId,
                cancellationToken);
            return Changed(concurrent?.Version);
        }

        return ApplicationResult<TripNotificationStateResult>.Success(
            await this.BuildStateAsync(access, current, cancellationToken));
    }

    private async Task<TripNotificationStateResult> BuildStateAsync(
        TripNotificationAccess access,
        CancellationToken cancellationToken)
    {
        TripNotificationSubscription? subscription = await this.subscriptions.GetAsync(
            access.Trip.Id,
            access.UserId,
            cancellationToken);
        return await this.BuildStateAsync(access, subscription, cancellationToken);
    }

    private async Task<TripNotificationStateResult> BuildStateAsync(
        TripNotificationAccess access,
        TripNotificationSubscription? subscription,
        CancellationToken cancellationToken)
    {
        if (subscription is null
            || subscription.MemberId != access.Member.Id
            || !subscription.IsEnabled)
        {
            return new TripNotificationStateResult(
                false,
                subscription is not null && subscription.MemberId == access.Member.Id
                    ? subscription.Version
                    : 0,
                0,
                false);
        }

        IReadOnlyCollection<TripActivityEvent> unread = await this.audit.ListImportantAfterAsync(
            access.Trip.Id,
            access.Member.Id,
            subscription.SeenThroughSequence,
            subscription.UpdatedAtUtc,
            TripNotificationPolicy.MaximumUnreadCount + 1,
            cancellationToken);
        return new TripNotificationStateResult(
            true,
            subscription.Version,
            Math.Min(unread.Count, TripNotificationPolicy.MaximumUnreadCount),
            unread.Count > TripNotificationPolicy.MaximumUnreadCount);
    }

    private async Task<TripNotificationAccess?> ResolveAccessAsync(
        string userId,
        string tripPlanId,
        CancellationToken cancellationToken)
    {
        try
        {
            string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            if (!TripPlanId.TryParse(tripPlanId, out TripPlanId parsedId))
            {
                return null;
            }

            TripPlan? trip = await this.plans.GetAccessibleAsync(
                normalizedUserId,
                parsedId,
                cancellationToken);
            TripMember? member = trip?.Members.SingleOrDefault(candidate =>
                candidate.State == TripMembershipState.Active
                && string.Equals(candidate.UserId, normalizedUserId, StringComparison.Ordinal));
            return trip is null || member is null
                ? null
                : new TripNotificationAccess(trip, member, normalizedUserId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private async Task<bool> IsStillAccessibleAsync(
        TripNotificationAccess access,
        CancellationToken cancellationToken)
    {
        TripPlan? current = await this.plans.GetAccessibleAsync(
            access.UserId,
            access.Trip.Id,
            cancellationToken);
        return current?.Members.Any(member =>
            member.State == TripMembershipState.Active
            && member.Id == access.Member.Id
            && string.Equals(member.UserId, access.UserId, StringComparison.Ordinal)) == true;
    }

    private static ApplicationResult<TripNotificationStateResult> NotFound()
    {
        return ApplicationResult<TripNotificationStateResult>.Failure(
            TripPlanApplicationErrors.NotFound());
    }

    private static ApplicationResult<TripNotificationStateResult> Invalid()
    {
        return ApplicationResult<TripNotificationStateResult>.Failure(
            TripPlanApplicationErrors.Invalid(
                TripPlanErrorCodes.InvalidVersion,
                "The trip notification version is invalid."));
    }

    private static ApplicationResult<TripNotificationStateResult> Changed(long? currentVersion)
    {
        return ApplicationResult<TripNotificationStateResult>.Failure(
            TripPlanApplicationErrors.NotificationChangedConcurrently(currentVersion));
    }
}
