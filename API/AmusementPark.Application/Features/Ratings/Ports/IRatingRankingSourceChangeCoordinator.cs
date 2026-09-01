using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingSourceChangeCoordinator
{
    Task<RatingRankingMutationPreparation> PrepareParkChangesAsync(
        IReadOnlyCollection<Park> previousParks,
        IReadOnlyCollection<Park> currentParks,
        CancellationToken cancellationToken);

    Task<RatingRankingMutationPreparation> PrepareParkItemChangesAsync(
        IReadOnlyCollection<ParkItem> previousItems,
        IReadOnlyCollection<ParkItem> currentItems,
        CancellationToken cancellationToken);

    Task CompleteMutationAsync(
        RatingRankingMutationPreparation preparation,
        bool sourceChanged,
        CancellationToken cancellationToken);
}
