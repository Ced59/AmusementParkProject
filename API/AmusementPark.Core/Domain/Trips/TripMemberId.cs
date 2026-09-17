namespace AmusementPark.Core.Domain.Trips;

public readonly record struct TripMemberId
{
    private readonly string? value;

    private TripMemberId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized trip member identifier has no value.");

    public static TripMemberId New()
    {
        return new TripMemberId(Guid.NewGuid().ToString("N"));
    }

    public static TripMemberId Parse(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A trip member identifier is required.", nameof(value));
        }

        return new TripMemberId(normalized);
    }
}
