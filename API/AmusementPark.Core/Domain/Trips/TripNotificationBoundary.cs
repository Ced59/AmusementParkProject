namespace AmusementPark.Core.Domain.Trips;

public sealed class TripNotificationBoundary
{
    public TripNotificationBoundary(
        long sequence,
        IEnumerable<string>? pendingOperationKeys = null)
    {
        if (sequence < 0)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The trip notification boundary is invalid.");
        }

        this.Sequence = sequence;
        this.PendingOperationKeys = (pendingOperationKeys ?? Array.Empty<string>())
            .Select(static value => value?.Trim() ?? string.Empty)
            .Where(static value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    public long Sequence { get; }

    public IReadOnlyCollection<string> PendingOperationKeys { get; }
}
