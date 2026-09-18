namespace AmusementPark.Core.Domain.Trips;

public readonly record struct TripInvitationId
{
    private readonly string? value;

    private TripInvitationId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized trip invitation identifier has no value.");

    public static TripInvitationId New()
    {
        return new TripInvitationId(Guid.NewGuid().ToString("N"));
    }

    public static TripInvitationId Parse(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A trip invitation identifier is required.", nameof(value));
        }

        return new TripInvitationId(normalized);
    }

    public static bool TryParse(string? value, out TripInvitationId invitationId)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            invitationId = default;
            return false;
        }

        invitationId = new TripInvitationId(normalized);
        return true;
    }
}
