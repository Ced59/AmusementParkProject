using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

internal sealed record HistoryTimelineIndexability(
    HashSet<string> ParkIds,
    HashSet<string> ParkItemIds,
    HashSet<string> StandaloneAttractionIds);
