namespace AmusementPark.Core.Domain.Visits;

public readonly record struct PassportBetaRepeatUsage
{
    public const int CandidateReturningUserCount = 3;

    private PassportBetaRepeatUsage(
        decimal ratePercent,
        PassportBetaRepeatUsageSignal signal)
    {
        this.RatePercent = ratePercent;
        this.Signal = signal;
    }

    public decimal RatePercent { get; }

    public PassportBetaRepeatUsageSignal Signal { get; }

    public bool RequiresQualitativeValidation => true;

    public static PassportBetaRepeatUsage FromCounts(
        long usersWithCompletedVisit,
        long usersWithSecondCompletedVisit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(usersWithCompletedVisit);
        ArgumentOutOfRangeException.ThrowIfNegative(usersWithSecondCompletedVisit);
        if (usersWithSecondCompletedVisit > usersWithCompletedVisit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usersWithSecondCompletedVisit),
                "Returning users must belong to the completed-visit cohort.");
        }

        decimal ratePercent = usersWithCompletedVisit == 0
            ? 0m
            : usersWithSecondCompletedVisit * 100m / usersWithCompletedVisit;
        PassportBetaRepeatUsageSignal signal = usersWithSecondCompletedVisit switch
        {
            <= 0 => PassportBetaRepeatUsageSignal.NotObserved,
            < CandidateReturningUserCount => PassportBetaRepeatUsageSignal.Emerging,
            _ => PassportBetaRepeatUsageSignal.Candidate,
        };
        return new PassportBetaRepeatUsage(ratePercent, signal);
    }
}
