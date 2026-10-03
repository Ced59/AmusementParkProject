namespace AmusementPark.Application.Features.History.Services;

internal static class HistoricalNarrativeCanonicalizationPolicy
{
    internal const string Version = "hist-canonical-v3";
    internal const string Actor = "system:hist-canonicalization";
    internal const string MethodologyVersion = "hist-v3-canonical";

    internal static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }
}
