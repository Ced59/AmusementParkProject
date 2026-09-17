namespace AmusementPark.Core.Domain.Trips;

public sealed class TripDayBlock
{
    public const int MaximumTitleLength = 120;
    public const int MaximumDetailsLength = 500;
    public const long SortPositionStep = 1024;

    public TripDayBlock(
        TripDayBlockId id,
        TripDayBlockType type,
        string title,
        string? details,
        TimeOnly? localTime,
        long sortPosition)
    {
        _ = id.Value;
        if (!Enum.IsDefined(type))
        {
            throw Invalid("The day block type is invalid.");
        }

        string normalizedTitle = title?.Trim() ?? string.Empty;
        string? normalizedDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        if (normalizedTitle.Length is 0 or > MaximumTitleLength)
        {
            throw Invalid($"A day block title is required and cannot exceed {MaximumTitleLength} characters.");
        }

        if (normalizedDetails?.Length > MaximumDetailsLength)
        {
            throw Invalid($"Day block details cannot exceed {MaximumDetailsLength} characters.");
        }

        this.Id = id;
        this.Type = type;
        this.Title = normalizedTitle;
        this.Details = normalizedDetails;
        this.LocalTime = localTime;
        this.SortPosition = sortPosition;
    }

    public TripDayBlockId Id { get; }

    public TripDayBlockType Type { get; }

    public string Title { get; }

    public string? Details { get; }

    public TimeOnly? LocalTime { get; }

    public long SortPosition { get; }

    private static TripPlanValidationException Invalid(string message)
    {
        return new TripPlanValidationException(TripPlanErrorCodes.InvalidDayPlan, message);
    }
}
