using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Models;

public sealed class PublicSeoUpdate
{
    public IReadOnlyCollection<PublicSeoParkSnapshot> PreviousParks { get; init; } = Array.Empty<PublicSeoParkSnapshot>();

    public IReadOnlyCollection<PublicSeoParkSnapshot> CurrentParks { get; init; } = Array.Empty<PublicSeoParkSnapshot>();

    public IReadOnlyCollection<PublicSeoParkItemSnapshot> PreviousParkItems { get; init; } = Array.Empty<PublicSeoParkItemSnapshot>();

    public IReadOnlyCollection<PublicSeoParkItemSnapshot> CurrentParkItems { get; init; } = Array.Empty<PublicSeoParkItemSnapshot>();

    public IReadOnlyCollection<PublicSeoParkZoneSnapshot> PreviousParkZones { get; init; } = Array.Empty<PublicSeoParkZoneSnapshot>();

    public IReadOnlyCollection<PublicSeoParkZoneSnapshot> CurrentParkZones { get; init; } = Array.Empty<PublicSeoParkZoneSnapshot>();

    public IReadOnlyCollection<PublicSeoVideoSnapshot> PreviousVideos { get; init; } = Array.Empty<PublicSeoVideoSnapshot>();

    public IReadOnlyCollection<PublicSeoVideoSnapshot> CurrentVideos { get; init; } = Array.Empty<PublicSeoVideoSnapshot>();

    public IReadOnlyCollection<PublicSeoImageSnapshot> PreviousImages { get; init; } = Array.Empty<PublicSeoImageSnapshot>();

    public IReadOnlyCollection<PublicSeoImageSnapshot> CurrentImages { get; init; } = Array.Empty<PublicSeoImageSnapshot>();

    public bool IncludeDiscoveryPages { get; init; }

    public bool SuppressSitemapRefresh { get; init; }
}
