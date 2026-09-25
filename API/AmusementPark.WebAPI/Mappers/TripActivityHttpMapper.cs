using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripActivityHttpMapper
{
    public static TripActivityPageDto ToHttp(this TripActivityPageResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripActivityPageDto(
            result.TripTitle,
            result.Entries.Select(static entry => new TripActivityEntryDto(
                entry.Sequence,
                entry.Kind.ToString(),
                entry.ActorDisplayName,
                entry.IsCurrentUser,
                entry.AffectedCount,
                entry.OccurredAtUtc)).ToArray(),
            result.NextBeforeSequence);
    }
}
