namespace AmusementPark.Core.Domain.Trips;

public static class TripProgramRules
{
    public static void ValidateCandidateDates(
        TripDateProposal proposal,
        IReadOnlyCollection<DateOnly> candidateDates)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentNullException.ThrowIfNull(candidateDates);
        if (proposal.Kind == TripDateProposalKind.None && candidateDates.Count > 0)
        {
            throw InvalidCandidate("Candidate dates require a dated trip proposal.");
        }

        foreach (DateOnly date in candidateDates)
        {
            if (!Contains(proposal, date))
            {
                throw InvalidCandidate("A park candidate date must belong to the trip date proposal.");
            }
        }
    }

    public static void ValidateDayDate(TripDateProposal proposal, DateOnly localDate)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        if (proposal.Kind != TripDateProposalKind.Fixed || !Contains(proposal, localDate))
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidDayPlan,
                "A decided day requires a fixed trip period containing that local date.");
        }
    }

    public static void ValidateDayCandidate(
        TripParkCandidate candidate,
        DateOnly localDate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (candidate.State != TripParkCandidateState.Selected)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidDayPlan,
                "A day can only use a selected park candidate.");
        }

        if (candidate.CandidateDates.Count > 0 && !candidate.CandidateDates.Contains(localDate))
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidDayPlan,
                "The selected park was not proposed for this day.");
        }
    }

    public static void ValidateCandidateDatesAgainstDays(
        TripParkCandidate candidate,
        IReadOnlyCollection<DateOnly> candidateDates,
        IReadOnlyCollection<TripDayPlan> days)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(candidateDates);
        ArgumentNullException.ThrowIfNull(days);
        if (candidateDates.Count > 0 && days.Any(day =>
            day.ParkCandidateId == candidate.Id && !candidateDates.Contains(day.LocalDate)))
        {
            throw InvalidCandidate(
                "A park assigned to a decided day must remain available on that date.");
        }
    }

    public static void ValidateCandidateStateAgainstDays(
        TripParkCandidate candidate,
        TripParkCandidateState state,
        IReadOnlyCollection<TripDayPlan> days)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(days);
        if (state != TripParkCandidateState.Selected
            && days.Any(day => day.ParkCandidateId == candidate.Id))
        {
            throw InvalidCandidate(
                "A park assigned to a decided day must remain selected.");
        }
    }

    public static void ValidateProgramAgainstProposal(
        TripDateProposal proposal,
        IReadOnlyCollection<TripParkCandidate> candidates,
        IReadOnlyCollection<TripDayPlan> days)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(days);
        Dictionary<TripParkCandidateId, TripParkCandidate> candidatesById = candidates.ToDictionary(
            static candidate => candidate.Id);
        foreach (TripParkCandidate candidate in candidates)
        {
            ValidateCandidateDates(proposal, candidate.CandidateDates);
        }

        foreach (TripDayPlan day in days)
        {
            ValidateDayDate(proposal, day.LocalDate);
            if (!candidatesById.TryGetValue(
                day.ParkCandidateId,
                out TripParkCandidate? candidate))
            {
                throw new TripPlanValidationException(
                    TripPlanErrorCodes.InvalidDayPlan,
                    "A decided day must reference an existing park candidate.");
            }

            ValidateDayCandidate(candidate, day.LocalDate);
        }
    }

    private static bool Contains(TripDateProposal proposal, DateOnly date)
    {
        return proposal.Kind switch
        {
            TripDateProposalKind.Fixed or TripDateProposalKind.Range =>
                date >= proposal.StartDate!.Value && date <= proposal.EndDate!.Value,
            TripDateProposalKind.Candidates => proposal.CandidateDates.Contains(date),
            _ => false,
        };
    }

    private static TripPlanValidationException InvalidCandidate(string message)
    {
        return new TripPlanValidationException(TripPlanErrorCodes.InvalidCandidate, message);
    }
}
