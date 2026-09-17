using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripDayPlan
{
    public const int MaximumGroupNoteLength = 2000;
    public const int MaximumBlocks = 50;
    public const int MaximumDaysPerTrip = TripDateProposal.MaximumRangeDays;

    private TripDayPlan(
        TripDayPlanId id,
        TripPlanId tripPlanId,
        DateOnly localDate,
        TripParkCandidateId parkCandidateId,
        string parkId,
        TimeOnly? desiredArrivalTime,
        string? groupNote,
        IReadOnlyCollection<TripDayBlock> blocks,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        _ = id.Value;
        _ = tripPlanId.Value;
        _ = parkCandidateId.Value;
        string normalizedParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        string? normalizedNote = NormalizeGroupNote(groupNote);
        TripDayBlock[] normalizedBlocks = NormalizeBlocks(blocks);
        if (version < 1)
        {
            throw Invalid("The day plan version must be positive.");
        }

        ValidateTimestamps(createdAtUtc, updatedAtUtc);
        this.Id = id;
        this.TripPlanId = tripPlanId;
        this.LocalDate = localDate;
        this.ParkCandidateId = parkCandidateId;
        this.ParkId = normalizedParkId;
        this.DesiredArrivalTime = desiredArrivalTime;
        this.GroupNote = normalizedNote;
        this.Blocks = normalizedBlocks;
        this.Version = version;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
    }

    public TripDayPlanId Id { get; }

    public TripPlanId TripPlanId { get; }

    public DateOnly LocalDate { get; }

    public TripParkCandidateId ParkCandidateId { get; private set; }

    public string ParkId { get; private set; }

    public TimeOnly? DesiredArrivalTime { get; private set; }

    public string? GroupNote { get; private set; }

    public IReadOnlyCollection<TripDayBlock> Blocks { get; private set; }

    public long Version { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static TripDayPlan Create(
        TripDayPlanId id,
        TripPlanId tripPlanId,
        DateOnly localDate,
        TripParkCandidateId parkCandidateId,
        string parkId,
        TimeOnly? desiredArrivalTime,
        string? groupNote,
        IReadOnlyCollection<TripDayBlock> blocks,
        DateTime nowUtc)
    {
        return new TripDayPlan(
            id,
            tripPlanId,
            localDate,
            parkCandidateId,
            parkId,
            desiredArrivalTime,
            groupNote,
            blocks,
            1,
            nowUtc,
            nowUtc);
    }

    public static TripDayPlan Restore(
        TripDayPlanId id,
        TripPlanId tripPlanId,
        DateOnly localDate,
        TripParkCandidateId parkCandidateId,
        string parkId,
        TimeOnly? desiredArrivalTime,
        string? groupNote,
        IReadOnlyCollection<TripDayBlock> blocks,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        return new TripDayPlan(
            id,
            tripPlanId,
            localDate,
            parkCandidateId,
            parkId,
            desiredArrivalTime,
            groupNote,
            blocks,
            version,
            createdAtUtc,
            updatedAtUtc);
    }

    public void Update(
        TripParkCandidateId parkCandidateId,
        string parkId,
        TimeOnly? desiredArrivalTime,
        string? groupNote,
        IReadOnlyCollection<TripDayBlock> blocks,
        DateTime nowUtc)
    {
        _ = parkCandidateId.Value;
        string normalizedParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        string? normalizedNote = NormalizeGroupNote(groupNote);
        TripDayBlock[] normalizedBlocks = NormalizeBlocks(blocks);
        this.ValidateMutation(nowUtc);
        if (this.ParkCandidateId == parkCandidateId
            && string.Equals(this.ParkId, normalizedParkId, StringComparison.Ordinal)
            && this.DesiredArrivalTime == desiredArrivalTime
            && string.Equals(this.GroupNote, normalizedNote, StringComparison.Ordinal)
            && BlocksEqual(this.Blocks, normalizedBlocks))
        {
            return;
        }

        if (this.Version == long.MaxValue)
        {
            throw Invalid("The day plan version cannot be incremented further.");
        }

        this.ParkCandidateId = parkCandidateId;
        this.ParkId = normalizedParkId;
        this.DesiredArrivalTime = desiredArrivalTime;
        this.GroupNote = normalizedNote;
        this.Blocks = normalizedBlocks;
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    private static string? NormalizeGroupNote(string? note)
    {
        string? normalized = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalized?.Length > MaximumGroupNoteLength)
        {
            throw Invalid($"A group note cannot exceed {MaximumGroupNoteLength} characters.");
        }

        return normalized;
    }

    private static TripDayBlock[] NormalizeBlocks(IReadOnlyCollection<TripDayBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        if (blocks.Count > MaximumBlocks || blocks.Select(static block => block.Id).Distinct().Count() != blocks.Count)
        {
            throw Invalid($"A day can contain at most {MaximumBlocks} uniquely identified blocks.");
        }

        long[] positions = blocks.Select(static block => block.SortPosition).ToArray();
        if (positions.Distinct().Count() != positions.Length)
        {
            throw Invalid("Day blocks must have unique sort positions.");
        }

        return blocks
            .OrderBy(static block => block.SortPosition)
            .ThenBy(static block => block.Id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool BlocksEqual(
        IReadOnlyCollection<TripDayBlock> left,
        IReadOnlyCollection<TripDayBlock> right)
    {
        return left.Count == right.Count && left.Zip(right).All(static pair =>
            pair.First.Id == pair.Second.Id
            && pair.First.Type == pair.Second.Type
            && string.Equals(pair.First.Title, pair.Second.Title, StringComparison.Ordinal)
            && string.Equals(pair.First.Details, pair.Second.Details, StringComparison.Ordinal)
            && pair.First.LocalTime == pair.Second.LocalTime
            && pair.First.SortPosition == pair.Second.SortPosition);
    }

    private void ValidateMutation(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw Invalid("A day plan mutation cannot predate its current state.");
        }
    }

    private static void ValidateTimestamps(DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (updatedAtUtc < createdAtUtc)
        {
            throw Invalid("Day plan timestamps must be chronological.");
        }
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid("Day plan timestamps must be expressed in UTC.");
        }
    }

    private static TripPlanValidationException Invalid(string message)
    {
        return new TripPlanValidationException(TripPlanErrorCodes.InvalidDayPlan, message);
    }
}
