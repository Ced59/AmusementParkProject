using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripParkCandidate
{
    public const int MaximumCandidatesPerTrip = 100;
    public const int MaximumCollectiveNoteLength = 1000;
    public const long SortPositionStep = 1024;

    private TripParkCandidate(
        TripParkCandidateId id,
        TripPlanId tripPlanId,
        string parkId,
        IReadOnlyCollection<DateOnly> candidateDates,
        TripParkCandidateSource source,
        TripParkCandidateState state,
        string? collectiveNote,
        TripFitRecommendationSnapshot? fitSnapshot,
        TripMemberId addedByMemberId,
        long sortPosition,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        _ = id.Value;
        _ = tripPlanId.Value;
        _ = addedByMemberId.Value;
        string normalizedParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        DateOnly[] normalizedDates = NormalizeDates(candidateDates);
        ValidateEnum(source, nameof(source));
        ValidateEnum(state, nameof(state));
        string? normalizedNote = NormalizeCollectiveNote(collectiveNote);
        ValidateVersion(version);
        ValidateTimestamps(createdAtUtc, updatedAtUtc);

        this.Id = id;
        this.TripPlanId = tripPlanId;
        this.ParkId = normalizedParkId;
        this.CandidateDates = normalizedDates;
        this.Source = source;
        this.State = state;
        this.CollectiveNote = normalizedNote;
        this.FitSnapshot = fitSnapshot;
        this.AddedByMemberId = addedByMemberId;
        this.SortPosition = sortPosition;
        this.Version = version;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
    }

    public TripParkCandidateId Id { get; }

    public TripPlanId TripPlanId { get; }

    public string ParkId { get; }

    public IReadOnlyCollection<DateOnly> CandidateDates { get; private set; }

    public TripParkCandidateSource Source { get; }

    public TripParkCandidateState State { get; private set; }

    public string? CollectiveNote { get; private set; }

    public TripFitRecommendationSnapshot? FitSnapshot { get; private set; }

    public TripMemberId AddedByMemberId { get; }

    public long SortPosition { get; private set; }

    public long Version { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static TripParkCandidate Create(
        TripParkCandidateId id,
        TripPlanId tripPlanId,
        string parkId,
        IReadOnlyCollection<DateOnly> candidateDates,
        TripParkCandidateSource source,
        string? collectiveNote,
        TripFitRecommendationSnapshot? fitSnapshot,
        TripMemberId addedByMemberId,
        long sortPosition,
        DateTime nowUtc)
    {
        return new TripParkCandidate(
            id,
            tripPlanId,
            parkId,
            candidateDates,
            source,
            TripParkCandidateState.Proposed,
            collectiveNote,
            fitSnapshot,
            addedByMemberId,
            sortPosition,
            1,
            nowUtc,
            nowUtc);
    }

    public static TripParkCandidate Restore(
        TripParkCandidateId id,
        TripPlanId tripPlanId,
        string parkId,
        IReadOnlyCollection<DateOnly> candidateDates,
        TripParkCandidateSource source,
        TripParkCandidateState state,
        string? collectiveNote,
        TripFitRecommendationSnapshot? fitSnapshot,
        TripMemberId addedByMemberId,
        long sortPosition,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        return new TripParkCandidate(
            id,
            tripPlanId,
            parkId,
            candidateDates,
            source,
            state,
            collectiveNote,
            fitSnapshot,
            addedByMemberId,
            sortPosition,
            version,
            createdAtUtc,
            updatedAtUtc);
    }

    public void UpdateDetails(
        IReadOnlyCollection<DateOnly> candidateDates,
        string? collectiveNote,
        TripFitRecommendationSnapshot? fitSnapshot,
        DateTime nowUtc)
    {
        DateOnly[] normalizedDates = NormalizeDates(candidateDates);
        string? normalizedNote = NormalizeCollectiveNote(collectiveNote);
        this.ValidateMutation(nowUtc);
        bool sameSnapshot = SnapshotsEqual(this.FitSnapshot, fitSnapshot);
        if (this.CandidateDates.SequenceEqual(normalizedDates)
            && string.Equals(this.CollectiveNote, normalizedNote, StringComparison.Ordinal)
            && sameSnapshot)
        {
            return;
        }

        this.PrepareMutation();
        this.CandidateDates = normalizedDates;
        this.CollectiveNote = normalizedNote;
        this.FitSnapshot = fitSnapshot;
        this.CommitMutation(nowUtc);
    }

    public void ChangeState(TripParkCandidateState state, DateTime nowUtc)
    {
        ValidateEnum(state, nameof(state));
        this.ValidateMutation(nowUtc);
        if (this.State == state)
        {
            return;
        }

        this.PrepareMutation();
        this.State = state;
        this.CommitMutation(nowUtc);
    }

    public void MoveTo(long sortPosition, DateTime nowUtc)
    {
        this.ValidateMutation(nowUtc);
        if (this.SortPosition == sortPosition)
        {
            return;
        }

        this.PrepareMutation();
        this.SortPosition = sortPosition;
        this.CommitMutation(nowUtc);
    }

    private static DateOnly[] NormalizeDates(IReadOnlyCollection<DateOnly> dates)
    {
        ArgumentNullException.ThrowIfNull(dates);
        if (dates.Count > TripDateProposal.MaximumCandidateDates)
        {
            throw Invalid(
                $"A park candidate cannot reference more than {TripDateProposal.MaximumCandidateDates} dates.");
        }

        return dates.Distinct().OrderBy(static date => date).ToArray();
    }

    private static string? NormalizeCollectiveNote(string? note)
    {
        string? normalized = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalized?.Length > MaximumCollectiveNoteLength)
        {
            throw Invalid($"A collective note cannot exceed {MaximumCollectiveNoteLength} characters.");
        }

        return normalized;
    }

    private static bool SnapshotsEqual(
        TripFitRecommendationSnapshot? left,
        TripFitRecommendationSnapshot? right)
    {
        return left is null && right is null
            || left is not null
            && right is not null
            && string.Equals(left.MethodVersion, right.MethodVersion, StringComparison.Ordinal)
            && string.Equals(left.Explanation, right.Explanation, StringComparison.Ordinal)
            && left.CalculatedAtUtc == right.CalculatedAtUtc;
    }

    private void ValidateMutation(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw Invalid("A candidate mutation cannot predate its current state.");
        }
    }

    private void PrepareMutation()
    {
        if (this.Version == long.MaxValue)
        {
            throw Invalid("The candidate version cannot be incremented further.");
        }
    }

    private void CommitMutation(DateTime nowUtc)
    {
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateVersion(long version)
    {
        if (version < 1)
        {
            throw Invalid("The candidate version must be positive.");
        }
    }

    private static void ValidateTimestamps(DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (updatedAtUtc < createdAtUtc)
        {
            throw Invalid("Candidate timestamps must be chronological.");
        }
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid("Candidate timestamps must be expressed in UTC.");
        }
    }

    private static TripPlanValidationException Invalid(string message)
    {
        return new TripPlanValidationException(TripPlanErrorCodes.InvalidCandidate, message);
    }
}
