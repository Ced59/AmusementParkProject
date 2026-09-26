using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Results;

public sealed record PublicParkHistoricalTimelineResult(
    Park Park,
    PagedResult<PublicHistoricalTimelineEntryResult> Page,
    IReadOnlyDictionary<string, string> ZoneNames);
