using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Services;
internal static class PublicSeoUrlResolverRoutesExtensions
{
    internal static void AddDiscoveryUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages)
    {
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/home");
            relativePaths.Add($"/{language}/parks");
        }
    }

    internal static void AddParkDetailUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}");
        }
    }

    internal static void AddParkImageUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/images");
        }
    }

    internal static void AddParkMapUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/map");
        }
    }

    internal static void AddParkHistoryUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/history");
        }
    }

    internal static void AddParkVideoUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/videos");
        }
    }

    internal static void AddParkVideoDetailUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park, PublicSeoVideoSnapshot video)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string videoSlug = SeoSlugService.ToSlug(video.Title, "video");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/videos/{video.Id}/{videoSlug}");
        }
    }

    internal static void AddParkItemListUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/items");
        }
    }

    internal static void AddParkItemDetailUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park, PublicSeoParkItemSnapshot item)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}");
        }
    }

    internal static void AddParkItemImageUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park, PublicSeoParkItemSnapshot item)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/images");
        }
    }

    internal static void AddParkItemHistoryUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park, PublicSeoParkItemSnapshot item)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/history");
        }
    }

    internal static void AddParkItemVideoUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park, PublicSeoParkItemSnapshot item)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/videos");
        }
    }

    internal static void AddParkItemVideoDetailUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park, PublicSeoParkItemSnapshot item, PublicSeoVideoSnapshot video)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        string videoSlug = SeoSlugService.ToSlug(video.Title, "video");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/videos/{video.Id}/{videoSlug}");
        }
    }

    internal static void AddZoneImpactUrls(HashSet<string> relativePaths, IReadOnlyCollection<string> languages, PublicSeoParkSnapshot park, IReadOnlyCollection<PublicSeoParkItemSnapshot> publicItems, IReadOnlyCollection<PublicSeoParkZoneSnapshot> zones)
    {
        HashSet<string> impactedZoneIds = publicItems.Select(static item => item.ZoneId).Where(static zoneId => !string.IsNullOrWhiteSpace(zoneId)).Select(static zoneId => zoneId!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (impactedZoneIds.Count == 0)
        {
            return;
        }

        List<PublicSeoParkZoneSnapshot> impactedZones = zones.Where(zone => impactedZoneIds.Contains(zone.Id) && PublicSeoUrlResolverRoutesExtensions.IsPublicZone(zone)).ToList();
        if (impactedZones.Count == 0)
        {
            return;
        }

        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        foreach (string language in languages)
        {
            relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/zones");
        }

        foreach (PublicSeoParkZoneSnapshot zone in impactedZones)
        {
            string zoneSlug = SeoSlugService.ToSlug(zone.Name, "zone");
            foreach (string language in languages)
            {
                relativePaths.Add($"/{language}/park/{park.Id}/{parkSlug}/zone/{zone.Id}/{zoneSlug}");
            }
        }
    }

    internal static bool IsPublicZone(PublicSeoParkZoneSnapshot zone)
    {
        return !string.IsNullOrWhiteSpace(zone.Id) && !string.IsNullOrWhiteSpace(zone.ParkId) && !string.IsNullOrWhiteSpace(zone.Name) && zone.IsVisible;
    }
}
