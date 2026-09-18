namespace AmusementPark.Core.Domain.Trips;

public sealed class TripPreferenceAggregate
{
    private TripPreferenceAggregate(
        int participantCount,
        int mustDoCount,
        int wantToDoCount,
        int optionalCount,
        int notForMeCount)
    {
        if (participantCount < 1
            || mustDoCount < 0
            || wantToDoCount < 0
            || optionalCount < 0
            || notForMeCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(participantCount));
        }

        int answeredCount = checked(mustDoCount + wantToDoCount + optionalCount + notForMeCount);
        if (answeredCount > participantCount)
        {
            throw new ArgumentException("Preference counts cannot exceed the active participant count.");
        }

        this.ParticipantCount = participantCount;
        this.MustDoCount = mustDoCount;
        this.WantToDoCount = wantToDoCount;
        this.OptionalCount = optionalCount;
        this.NotForMeCount = notForMeCount;
        this.UnansweredCount = participantCount - answeredCount;
        this.Compatibility = ResolveCompatibility(
            participantCount,
            answeredCount,
            mustDoCount + wantToDoCount + optionalCount,
            notForMeCount);
    }

    public int ParticipantCount { get; }

    public int MustDoCount { get; }

    public int WantToDoCount { get; }

    public int OptionalCount { get; }

    public int NotForMeCount { get; }

    public int UnansweredCount { get; }

    public TripPreferenceCompatibility Compatibility { get; }

    public bool IsCompatibilityKnown => this.UnansweredCount == 0;

    public bool HasIndividualConstraint => this.NotForMeCount > 0;

    public bool IsGroupPriority => this.MustDoCount > 0 && this.NotForMeCount == 0;

    public static TripPreferenceAggregate Create(
        int participantCount,
        int mustDoCount,
        int wantToDoCount,
        int optionalCount,
        int notForMeCount)
    {
        return new TripPreferenceAggregate(
            participantCount,
            mustDoCount,
            wantToDoCount,
            optionalCount,
            notForMeCount);
    }

    private static TripPreferenceCompatibility ResolveCompatibility(
        int participantCount,
        int answeredCount,
        int positiveCount,
        int notForMeCount)
    {
        if (positiveCount > 0 && notForMeCount > 0)
        {
            return TripPreferenceCompatibility.Conflict;
        }

        if (answeredCount == 0)
        {
            return TripPreferenceCompatibility.Unknown;
        }

        if (answeredCount == participantCount && positiveCount == participantCount)
        {
            return TripPreferenceCompatibility.Consensus;
        }

        return TripPreferenceCompatibility.Mixed;
    }
}
