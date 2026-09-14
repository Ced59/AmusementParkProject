using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkFit.Services;

/// <summary>
/// Déduplique uniquement les preuves strictement identiques dans une réponse de recherche.
/// </summary>
internal sealed class ParkFitCriticalSourceReferenceComparer
    : IEqualityComparer<AttractionCompatibilitySourceReference>
{
    public static ParkFitCriticalSourceReferenceComparer Instance { get; } = new();

    private ParkFitCriticalSourceReferenceComparer()
    {
    }

    public bool Equals(
        AttractionCompatibilitySourceReference? left,
        AttractionCompatibilitySourceReference? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Kind == right.Kind
            && string.Equals(left.Url, right.Url, StringComparison.Ordinal)
            && string.Equals(left.Reference, right.Reference, StringComparison.Ordinal)
            && string.Equals(left.LanguageCode, right.LanguageCode, StringComparison.Ordinal)
            && left.CollectedAtUtc == right.CollectedAtUtc
            && left.CollectedAtUtc?.Kind == right.CollectedAtUtc?.Kind
            && left.VerifiedAtUtc == right.VerifiedAtUtc
            && left.VerifiedAtUtc?.Kind == right.VerifiedAtUtc?.Kind
            && left.Confidence == right.Confidence
            && left.Summaries.SequenceEqual(right.Summaries, LocalizedTextComparer.Instance);
    }

    public int GetHashCode(AttractionCompatibilitySourceReference source)
    {
        ArgumentNullException.ThrowIfNull(source);

        HashCode hash = new HashCode();
        hash.Add(source.Kind);
        hash.Add(source.Url, StringComparer.Ordinal);
        hash.Add(source.Reference, StringComparer.Ordinal);
        hash.Add(source.LanguageCode, StringComparer.Ordinal);
        hash.Add(source.CollectedAtUtc);
        hash.Add(source.CollectedAtUtc?.Kind);
        hash.Add(source.VerifiedAtUtc);
        hash.Add(source.VerifiedAtUtc?.Kind);
        hash.Add(source.Confidence);
        foreach (LocalizedText summary in source.Summaries)
        {
            hash.Add(summary.LanguageCode, StringComparer.Ordinal);
            hash.Add(summary.Value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
