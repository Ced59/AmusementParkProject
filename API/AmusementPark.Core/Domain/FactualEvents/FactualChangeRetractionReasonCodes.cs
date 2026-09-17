namespace AmusementPark.Core.Domain.FactualEvents;

public static class FactualChangeRetractionReasonCodes
{
    public const string SourceInvalidated = "source-invalidated";
    public const string ChangeCancelled = "change-cancelled";
    public const string DuplicateEvent = "duplicate-event";
    public const string PublishedInError = "published-in-error";

    private static readonly IReadOnlySet<string> Supported = new HashSet<string>(
        new[]
        {
            SourceInvalidated,
            ChangeCancelled,
            DuplicateEvent,
            PublishedInError,
        },
        StringComparer.Ordinal);

    public static bool IsSupported(string reasonCode)
    {
        return Supported.Contains(reasonCode);
    }
}
