namespace AmusementPark.Application.Features.Ratings.Models;

public sealed class RatingRankingMutationPreparation
{
    public RatingRankingMutationPreparation(
        IReadOnlyCollection<RatingRankingMutationLease> mutationLeases)
    {
        ArgumentNullException.ThrowIfNull(mutationLeases);
        this.MutationLeases = Array.AsReadOnly(mutationLeases
            .DistinctBy(static lease => lease.ScopeKey)
            .OrderBy(static lease => lease.ScopeKey.Value, StringComparer.Ordinal)
            .ToArray());
    }

    public IReadOnlyCollection<RatingRankingMutationLease> MutationLeases { get; }
}
