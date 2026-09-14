namespace AmusementPark.Core.Domain.ParkFit;

public sealed class ParkFitOperationalStatus
{
    public const int MaximumReasonLength = 500;
    public const int MaximumDecisionHistoryCount = 50;

    private readonly List<ParkFitOperationalDecision> decisions;

    private ParkFitOperationalStatus(
        string parkId,
        ParkFitRecommendationState state,
        long revision,
        DateTime? updatedAtUtc,
        IEnumerable<ParkFitOperationalDecision> decisions)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        List<ParkFitOperationalDecision> normalizedDecisions = decisions?.ToList()
            ?? throw new ArgumentNullException(nameof(decisions));
        if (normalizedParkId.Length == 0
            || normalizedParkId.Length > 200
            || !Enum.IsDefined(state)
            || revision < 0
            || updatedAtUtc.HasValue && updatedAtUtc.Value.Kind != DateTimeKind.Utc
            || normalizedDecisions.Count > MaximumDecisionHistoryCount
            || normalizedDecisions.Any(static decision => decision is null)
            || normalizedDecisions.Count == 0
                && (revision != 0
                    || state is not ParkFitRecommendationState.Active
                        and not ParkFitRecommendationState.NotActivated
                    || updatedAtUtc.HasValue)
            || normalizedDecisions.Count > 0
                && (normalizedDecisions[^1].Revision != revision
                    || normalizedDecisions[^1].DecidedAtUtc != updatedAtUtc))
        {
            throw new ArgumentException("The Park Fit operational status is invalid.");
        }

        ParkFitRecommendationState expectedState = normalizedDecisions.Count == 0
            ? state
            : GetRequiredState(normalizedDecisions[0].Type);
        long expectedRevision = Math.Max(1, revision - normalizedDecisions.Count + 1);
        foreach (ParkFitOperationalDecision decision in normalizedDecisions)
        {
            if (decision.Revision != expectedRevision
                || GetRequiredState(decision.Type) != expectedState)
            {
                throw new ArgumentException("The Park Fit operational history is invalid.");
            }

            expectedState = GetTargetState(decision.Type);
            expectedRevision++;
        }

        if (normalizedDecisions.Count > 0 && expectedState != state)
        {
            throw new ArgumentException("The Park Fit operational state does not match its history.");
        }

        this.ParkId = normalizedParkId;
        this.State = state;
        this.Revision = revision;
        this.UpdatedAtUtc = updatedAtUtc;
        this.decisions = normalizedDecisions;
    }

    public string ParkId { get; }

    public ParkFitRecommendationState State { get; private set; }

    public long Revision { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ParkFitOperationalDecision> Decisions => this.decisions.AsReadOnly();

    public static ParkFitOperationalStatus CreateActive(string parkId)
    {
        return new ParkFitOperationalStatus(
            parkId,
            ParkFitRecommendationState.Active,
            0,
            null,
            Array.Empty<ParkFitOperationalDecision>());
    }

    public static ParkFitOperationalStatus CreateNotActivated(string parkId)
    {
        return new ParkFitOperationalStatus(
            parkId,
            ParkFitRecommendationState.NotActivated,
            0,
            null,
            Array.Empty<ParkFitOperationalDecision>());
    }

    public static ParkFitOperationalStatus Restore(
        string parkId,
        ParkFitRecommendationState state,
        long revision,
        DateTime? updatedAtUtc,
        IEnumerable<ParkFitOperationalDecision> decisions)
    {
        return new ParkFitOperationalStatus(parkId, state, revision, updatedAtUtc, decisions);
    }

    public void Suspend(string actorUserId, string reason, DateTime decidedAtUtc)
    {
        this.Apply(
            ParkFitRecommendationState.Active,
            ParkFitRecommendationState.Suspended,
            ParkFitOperationalDecisionType.Suspended,
            actorUserId,
            reason,
            decidedAtUtc);
    }

    public void RestoreRecommendations(string actorUserId, string reason, DateTime decidedAtUtc)
    {
        this.Apply(
            ParkFitRecommendationState.Suspended,
            ParkFitRecommendationState.Active,
            ParkFitOperationalDecisionType.Restored,
            actorUserId,
            reason,
            decidedAtUtc);
    }

    public void Activate(string actorUserId, string reason, DateTime decidedAtUtc)
    {
        this.Apply(
            ParkFitRecommendationState.NotActivated,
            ParkFitRecommendationState.Active,
            ParkFitOperationalDecisionType.Activated,
            actorUserId,
            reason,
            decidedAtUtc);
    }

    public void Deactivate(string actorUserId, string reason, DateTime decidedAtUtc)
    {
        ParkFitRecommendationState requiredState = this.State == ParkFitRecommendationState.Suspended
            ? ParkFitRecommendationState.Suspended
            : ParkFitRecommendationState.Active;
        ParkFitOperationalDecisionType decisionType =
            requiredState == ParkFitRecommendationState.Suspended
                ? ParkFitOperationalDecisionType.DeactivatedDuringSuspension
                : ParkFitOperationalDecisionType.Deactivated;
        this.Apply(
            requiredState,
            ParkFitRecommendationState.NotActivated,
            decisionType,
            actorUserId,
            reason,
            decidedAtUtc);
    }

    private void Apply(
        ParkFitRecommendationState requiredState,
        ParkFitRecommendationState targetState,
        ParkFitOperationalDecisionType decisionType,
        string actorUserId,
        string reason,
        DateTime decidedAtUtc)
    {
        if (this.State != requiredState || this.Revision == long.MaxValue)
        {
            throw new InvalidOperationException("This Park Fit operational transition is not allowed.");
        }

        DateTime earliestUtc = this.UpdatedAtUtc
            ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        if (decidedAtUtc.Kind != DateTimeKind.Utc || decidedAtUtc < earliestUtc)
        {
            throw new ArgumentException("The decision timestamp is invalid.", nameof(decidedAtUtc));
        }

        long nextRevision = this.Revision + 1;
        ParkFitOperationalDecision decision = new ParkFitOperationalDecision(
            decisionType,
            actorUserId,
            reason,
            decidedAtUtc,
            nextRevision);
        this.decisions.Add(decision);
        if (this.decisions.Count > MaximumDecisionHistoryCount)
        {
            this.decisions.RemoveAt(0);
        }

        this.State = targetState;
        this.Revision = nextRevision;
        this.UpdatedAtUtc = decidedAtUtc;
    }

    private static ParkFitRecommendationState GetRequiredState(
        ParkFitOperationalDecisionType decisionType)
    {
        return decisionType switch
        {
            ParkFitOperationalDecisionType.Suspended => ParkFitRecommendationState.Active,
            ParkFitOperationalDecisionType.Restored => ParkFitRecommendationState.Suspended,
            ParkFitOperationalDecisionType.Activated => ParkFitRecommendationState.NotActivated,
            ParkFitOperationalDecisionType.Deactivated => ParkFitRecommendationState.Active,
            ParkFitOperationalDecisionType.DeactivatedDuringSuspension =>
                ParkFitRecommendationState.Suspended,
            _ => throw new ArgumentOutOfRangeException(nameof(decisionType)),
        };
    }

    private static ParkFitRecommendationState GetTargetState(
        ParkFitOperationalDecisionType decisionType)
    {
        return decisionType switch
        {
            ParkFitOperationalDecisionType.Suspended => ParkFitRecommendationState.Suspended,
            ParkFitOperationalDecisionType.Restored => ParkFitRecommendationState.Active,
            ParkFitOperationalDecisionType.Activated => ParkFitRecommendationState.Active,
            ParkFitOperationalDecisionType.Deactivated => ParkFitRecommendationState.NotActivated,
            ParkFitOperationalDecisionType.DeactivatedDuringSuspension =>
                ParkFitRecommendationState.NotActivated,
            _ => throw new ArgumentOutOfRangeException(nameof(decisionType)),
        };
    }
}
