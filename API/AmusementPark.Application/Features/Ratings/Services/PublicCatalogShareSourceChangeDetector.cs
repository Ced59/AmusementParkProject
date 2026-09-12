using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Services;

internal static class PublicCatalogShareSourceChangeDetector
{
    public static IReadOnlyCollection<string> ResolveParkScopes(
        string parkId,
        Park? previous,
        Park? current)
    {
        HashSet<string> scopes = new HashSet<string>(StringComparer.Ordinal);
        bool previousAvailable = IsPassportParkAvailable(previous);
        bool currentAvailable = IsPassportParkAvailable(current);
        bool availabilityChanged = previousAvailable != currentAvailable;
        if (availabilityChanged)
        {
            scopes.Add(PublicCatalogShareSourceScope.CreatePassportActivityPark(parkId));
            scopes.Add(PublicCatalogShareSourceScope.CreatePassportMissedItemsPark(parkId));
        }

        if (availabilityChanged
            || (previousAvailable
                && currentAvailable
                && (!NamesHaveEquivalentPublicLabel(previous!.Name, current!.Name)
                    || !string.Equals(
                        NormalizeCountryCode(previous.CountryCode),
                        NormalizeCountryCode(current.CountryCode),
                        StringComparison.Ordinal))))
        {
            scopes.Add(PublicCatalogShareSourceScope.CreatePassportGeographyPark(parkId));
        }

        bool previousRatingEligible = IsParkIncluded(previous);
        bool currentRatingEligible = IsParkIncluded(current);
        if (previousRatingEligible != currentRatingEligible
            || (previousRatingEligible
                && currentRatingEligible
                && !NamesHaveEquivalentPublicLabel(previous!.Name, current!.Name)))
        {
            scopes.Add(PublicCatalogShareSourceScope.CreatePassportRatingsPark(parkId));
        }

        return scopes.ToArray();
    }

    public static IReadOnlyCollection<string> ResolveParkItemScopes(
        ParkItem? previous,
        ParkItem? current)
    {
        HashSet<string> scopes = new HashSet<string>(StringComparer.Ordinal);
        bool previousAvailable = previous is not null && previous.IsVisible;
        bool currentAvailable = current is not null && current.IsVisible;
        bool placementChanged = previousAvailable
            && currentAvailable
            && !string.Equals(
                previous!.ParkId?.Trim(),
                current!.ParkId?.Trim(),
                StringComparison.Ordinal);
        bool availabilityChanged = previousAvailable != currentAvailable || placementChanged;
        if (availabilityChanged)
        {
            AddParkScope(
                scopes,
                previous?.ParkId,
                PublicCatalogShareSourceScope.CreatePassportActivityPark);
            AddParkScope(
                scopes,
                current?.ParkId,
                PublicCatalogShareSourceScope.CreatePassportActivityPark);
        }

        bool missedItemsChanged = availabilityChanged
            || (previousAvailable
                && currentAvailable
                && !NamesHaveEquivalentPublicLabel(previous!.Name, current!.Name));
        if (missedItemsChanged)
        {
            AddParkScope(
                scopes,
                previous?.ParkId,
                PublicCatalogShareSourceScope.CreatePassportMissedItemsPark);
            AddParkScope(
                scopes,
                current?.ParkId,
                PublicCatalogShareSourceScope.CreatePassportMissedItemsPark);
        }

        bool previousRatingEligible = IsParkItemIncluded(previous);
        bool currentRatingEligible = IsParkItemIncluded(current);
        bool ratingsChanged = previousRatingEligible != currentRatingEligible
            || (previousRatingEligible
                && currentRatingEligible
                && (!NamesHaveEquivalentPublicLabel(previous!.Name, current!.Name)
                    || placementChanged
                    || previous.Category != current.Category
                    || previous.AttractionDetails?.ClosingDate
                        != current.AttractionDetails?.ClosingDate));
        if (ratingsChanged)
        {
            AddParkScope(
                scopes,
                previous?.ParkId,
                PublicCatalogShareSourceScope.CreatePassportRatingsPark);
            AddParkScope(
                scopes,
                current?.ParkId,
                PublicCatalogShareSourceScope.CreatePassportRatingsPark);
        }

        return scopes.ToArray();
    }

    private static bool IsPassportParkAvailable(Park? park)
    {
        return park is not null
            && park.IsVisible
            && !string.IsNullOrWhiteSpace(park.Name);
    }

    private static bool IsParkIncluded(Park? park)
    {
        return park is not null
            && park.IsVisible
            && park.Status.CanAppearInCurrentRatingRankings();
    }

    private static bool IsParkItemIncluded(ParkItem? item)
    {
        return item is not null
            && item.IsVisible
            && ParkItemStatusNormalizer.CanAppearInCurrentRatingRankings(
                item.Category,
                item.AttractionDetails?.Status);
    }

    private static bool NamesHaveEquivalentPublicLabel(
        string? previousName,
        string? currentName)
    {
        return string.Equals(
            previousName?.Trim(),
            currentName?.Trim(),
            StringComparison.Ordinal);
    }

    private static string? NormalizeCountryCode(string? value)
    {
        string normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }

    private static void AddParkScope(
        ISet<string> scopes,
        string? parkId,
        Func<string, string> createScope)
    {
        if (!string.IsNullOrWhiteSpace(parkId))
        {
            scopes.Add(createScope(parkId));
        }
    }
}
