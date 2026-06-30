using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed partial class PublicSeoUrlResolver
{
    private static void AddDiscoveryUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages)
    {
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/home");
            relativePaths.Add($"/{language}/parks");
        }
    }

    private static void AddParkDetailUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}");
        }
    }

    private static void AddParkImageUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/images");
        }
    }

    private static void AddParkHistoryUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/history");
        }
    }

    private static void AddParkVideoUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/videos");
        }
    }

    private static void AddParkVideoDetailUrls(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        PublicSeoVideoSnapshot video)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string videoSlug = SeoSlugService.ToSlug(video.Title, "video");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/videos/{video.Id}/{videoSlug}");
        }
    }

    private static void AddParkItemListUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/items");
        }
    }

    private static void AddParkItemDetailUrls(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        PublicSeoParkItemSnapshot item)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}");
        }
    }

    private static void AddParkItemImageUrls(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        PublicSeoParkItemSnapshot item)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/images");
        }
    }

    private static void AddParkItemHistoryUrls(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        PublicSeoParkItemSnapshot item)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/history");
        }
    }

    private static void AddParkItemVideoUrls(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        PublicSeoParkItemSnapshot item)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/videos");
        }
    }

    private static void AddParkItemVideoDetailUrls(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        PublicSeoParkItemSnapshot item,
        PublicSeoVideoSnapshot video)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        string videoSlug = SeoSlugService.ToSlug(video.Title, "video");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/videos/{video.Id}/{videoSlug}");
        }
    }

    private static void AddZoneImpactUrls(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        PublicSeoParkSnapshot park,
        IReadOnlyCollection<PublicSeoParkItemSnapshot> publicItems,
        IReadOnlyCollection<ParkZone> zones)
    {
        HashSet<string> impactedZoneIds = publicItems
            .Select(static item => item.ZoneId)
            .Where(static zoneId => !string.IsNullOrWhiteSpace(zoneId))
            .Select(static zoneId => zoneId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (impactedZoneIds.Count == 0)
        {
            return;
        }

        List<ParkZone> impactedZones = zones
            .Where(zone => !string.IsNullOrWhiteSpace(zone.Id) && impactedZoneIds.Contains(zone.Id) && ParkZonesSitemapSectionProvider.IsPublicZone(zone))
            .ToList();
        if (impactedZones.Count == 0)
        {
            return;
        }

        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/zones");
        }

        foreach (ParkZone zone in impactedZones)
        {
            string zoneSlug = SeoSlugService.ToSlug(zone.Name, "zone");
            foreach (string language in languages)
            {
                relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/zone/{zone.Id}/{zoneSlug}");
            }
        }
    }
}
