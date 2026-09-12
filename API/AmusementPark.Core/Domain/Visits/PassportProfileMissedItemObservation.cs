namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportProfileMissedItemObservation
{
    public PassportProfileMissedItemObservation(string name, RideOccurrenceStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A public item name is required.", nameof(name));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        this.Name = name.Trim();
        this.Status = status;
    }

    public string Name { get; }

    public RideOccurrenceStatus Status { get; }
}
