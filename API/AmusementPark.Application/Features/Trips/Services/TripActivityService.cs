using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripActivityService
{
    public const int PageSize = 30;
    private const string NeutralDisplayName = "—";
    private readonly ITripPlanRepository plans;
    private readonly ITripAuditReader auditReader;
    private readonly IUserRepository users;

    public TripActivityService(
        ITripPlanRepository plans,
        ITripAuditReader auditReader,
        IUserRepository users)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.auditReader = auditReader ?? throw new ArgumentNullException(nameof(auditReader));
        this.users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<ApplicationResult<TripActivityPageResult>> GetAsync(
        string userId,
        string tripPlanId,
        long? beforeSequence,
        CancellationToken cancellationToken)
    {
        if (!TryNormalize(userId, tripPlanId, beforeSequence, out string normalizedUserId, out TripPlanId parsedId))
        {
            return ApplicationResult<TripActivityPageResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        TripPlan? trip = await this.plans.GetAccessibleAsync(normalizedUserId, parsedId, cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripActivityPageResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        IReadOnlyCollection<TripActivityEvent> events = await this.auditReader.ListAsync(
            parsedId,
            beforeSequence,
            PageSize + 1,
            cancellationToken);
        TripActivityEvent[] page = events.Take(PageSize).ToArray();
        IReadOnlyDictionary<TripMemberId, TripMember> activeMembersById = trip.Members
            .Where(static member => member.State == TripMembershipState.Active)
            .ToDictionary(static member => member.Id);
        string[] visibleActorIds = page
            .Select(activity => activity.ActorMemberId.HasValue
                && activeMembersById.TryGetValue(activity.ActorMemberId.Value, out TripMember? member)
                    ? member.UserId
                    : null)
            .Where(static userId => userId is not null)
            .Select(static userId => userId!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<User> resolvedUsers = visibleActorIds.Length == 0
            ? Array.Empty<User>()
            : await this.users.GetByIdsAsync(visibleActorIds, cancellationToken);
        IReadOnlyDictionary<string, User> usersById = resolvedUsers.ToDictionary(
            static user => user.Id,
            StringComparer.Ordinal);
        TripActivityEntryResult[] entries = page.Select(activity => new TripActivityEntryResult(
                activity.Sequence,
                activity.Kind,
                ResolveDisplayName(usersById, activeMembersById, activity.ActorMemberId),
                IsCurrentUser(activeMembersById, activity.ActorMemberId, normalizedUserId),
                activity.AffectedCount,
                activity.OccurredAtUtc))
            .ToArray();
        return ApplicationResult<TripActivityPageResult>.Success(new TripActivityPageResult(
            trip.Title,
            entries,
            events.Count > PageSize && page.Length > 0 ? page[^1].Sequence : null));
    }

    private static string ResolveDisplayName(
        IReadOnlyDictionary<string, User> users,
        IReadOnlyDictionary<TripMemberId, TripMember> activeMembers,
        TripMemberId? actorMemberId)
    {
        return actorMemberId.HasValue
            && activeMembers.TryGetValue(actorMemberId.Value, out TripMember? member)
            && users.TryGetValue(member.UserId, out User? user)
            ? user.ResolvePublicDisplayName() ?? NeutralDisplayName
            : NeutralDisplayName;
    }

    private static bool IsCurrentUser(
        IReadOnlyDictionary<TripMemberId, TripMember> activeMembers,
        TripMemberId? actorMemberId,
        string currentUserId)
    {
        return actorMemberId.HasValue
            && activeMembers.TryGetValue(actorMemberId.Value, out TripMember? member)
            && string.Equals(member.UserId, currentUserId, StringComparison.Ordinal);
    }

    private static bool TryNormalize(
        string userId,
        string tripPlanId,
        long? beforeSequence,
        out string normalizedUserId,
        out TripPlanId parsedId)
    {
        normalizedUserId = string.Empty;
        parsedId = default;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            return (!beforeSequence.HasValue || beforeSequence.Value > 0)
                && TripPlanId.TryParse(tripPlanId, out parsedId);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
