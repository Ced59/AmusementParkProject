namespace AmusementPark.Core.Domain.Trips;

public sealed class TripDateProposal
{
    public const int MaximumCandidateDates = 31;
    public const int MaximumRangeDays = 366;

    private TripDateProposal(
        TripDateProposalKind kind,
        DateOnly? startDate,
        DateOnly? endDate,
        IReadOnlyCollection<DateOnly> candidateDates)
    {
        if (!Enum.IsDefined(kind))
        {
            throw Invalid("The trip date proposal kind is invalid.");
        }

        DateOnly[] normalizedCandidates = candidateDates
            .Distinct()
            .OrderBy(static date => date)
            .ToArray();
        ValidateShape(kind, startDate, endDate, normalizedCandidates);
        this.Kind = kind;
        this.StartDate = startDate;
        this.EndDate = endDate;
        this.CandidateDates = normalizedCandidates;
    }

    public TripDateProposalKind Kind { get; }

    public DateOnly? StartDate { get; }

    public DateOnly? EndDate { get; }

    public IReadOnlyCollection<DateOnly> CandidateDates { get; }

    public static TripDateProposal None()
    {
        return new TripDateProposal(TripDateProposalKind.None, null, null, Array.Empty<DateOnly>());
    }

    public static TripDateProposal Fixed(DateOnly date)
    {
        return new TripDateProposal(TripDateProposalKind.Fixed, date, null, Array.Empty<DateOnly>());
    }

    public static TripDateProposal Range(DateOnly startDate, DateOnly endDate)
    {
        return new TripDateProposal(TripDateProposalKind.Range, startDate, endDate, Array.Empty<DateOnly>());
    }

    public static TripDateProposal Candidates(IReadOnlyCollection<DateOnly> dates)
    {
        ArgumentNullException.ThrowIfNull(dates);
        return new TripDateProposal(TripDateProposalKind.Candidates, null, null, dates);
    }

    public static TripDateProposal Restore(
        TripDateProposalKind kind,
        DateOnly? startDate,
        DateOnly? endDate,
        IReadOnlyCollection<DateOnly> candidateDates)
    {
        ArgumentNullException.ThrowIfNull(candidateDates);
        return new TripDateProposal(kind, startDate, endDate, candidateDates);
    }

    public bool HasSameValueAs(TripDateProposal other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return this.Kind == other.Kind
            && this.StartDate == other.StartDate
            && this.EndDate == other.EndDate
            && this.CandidateDates.SequenceEqual(other.CandidateDates);
    }

    private static void ValidateShape(
        TripDateProposalKind kind,
        DateOnly? startDate,
        DateOnly? endDate,
        IReadOnlyCollection<DateOnly> candidates)
    {
        bool valid = kind switch
        {
            TripDateProposalKind.None => !startDate.HasValue && !endDate.HasValue && candidates.Count == 0,
            TripDateProposalKind.Fixed => startDate.HasValue && !endDate.HasValue && candidates.Count == 0,
            TripDateProposalKind.Range => startDate.HasValue && endDate.HasValue && candidates.Count == 0,
            TripDateProposalKind.Candidates => !startDate.HasValue && !endDate.HasValue
                && candidates.Count is > 0 and <= MaximumCandidateDates,
            _ => false,
        };
        if (!valid)
        {
            throw Invalid("The trip date proposal does not match its selected mode.");
        }

        if (kind == TripDateProposalKind.Range
            && (endDate!.Value < startDate!.Value
                || endDate.Value.DayNumber - startDate.Value.DayNumber + 1 > MaximumRangeDays))
        {
            throw Invalid($"A trip date range must be chronological and no longer than {MaximumRangeDays} days.");
        }
    }

    private static TripPlanValidationException Invalid(string message)
    {
        return new TripPlanValidationException(TripPlanErrorCodes.InvalidDateProposal, message);
    }
}
