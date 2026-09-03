using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class RideOccurrenceOrderLoader
{
    public static async Task<IReadOnlyList<RideOccurrence>> LoadAllAsync(
        IRideOccurrenceRepository repository,
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        List<RideOccurrence> occurrences = new List<RideOccurrence>();
        RideOccurrenceListCursor? cursor = null;
        do
        {
            RideOccurrencePage page = await repository.ListOwnedByVisitAsync(
                new RideOccurrenceListCriteria(
                    visitId,
                    userId,
                    RideOccurrenceListCriteria.MaximumLimit,
                    cursor),
                cancellationToken);
            occurrences.AddRange(page.Items);
            cursor = page.NextCursor;
            if (occurrences.Count > RideOccurrenceOrderPlanner.MaximumReorderSize)
            {
                throw new InvalidOperationException(
                    "The visit exceeds the supported bounded reorder size.");
            }
        }
        while (cursor is not null);

        return occurrences;
    }
}
