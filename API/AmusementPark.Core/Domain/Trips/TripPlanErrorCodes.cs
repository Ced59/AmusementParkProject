namespace AmusementPark.Core.Domain.Trips;

public static class TripPlanErrorCodes
{
    public const string InvalidState = "trip.plan.invalid-state";
    public const string InvalidTitle = "trip.plan.invalid-title";
    public const string InvalidDateProposal = "trip.plan.invalid-date-proposal";
    public const string InvalidTimeZone = "trip.plan.invalid-time-zone";
    public const string InvalidVersion = "trip.plan.invalid-version";
    public const string InvalidTimestamp = "trip.plan.invalid-timestamp";
    public const string InvalidOwner = "trip.plan.invalid-owner";
    public const string InvalidCandidate = "trip.plan.invalid-candidate";
    public const string InvalidPreference = "trip.plan.invalid-preference";
    public const string InvalidDayPlan = "trip.plan.invalid-day-plan";
    public const string ProgramLimitReached = "trip.plan.program-limit-reached";
    public const string InvalidChildMutationLease = "trip.plan.invalid-child-mutation-lease";
}
