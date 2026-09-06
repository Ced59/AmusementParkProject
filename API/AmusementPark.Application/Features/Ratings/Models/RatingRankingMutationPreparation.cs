using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed class RatingRankingMutationPreparation
{
    public RatingRankingMutationPreparation(
        IReadOnlyCollection<RatingRankingMutationLease> mutationLeases,
        ShareSourceMutationLease? personalRankingShareMutationLease = null,
        ShareSourceMutationLease? personalRankingCatalogMutationLease = null)
    {
        ArgumentNullException.ThrowIfNull(mutationLeases);
        this.MutationLeases = Array.AsReadOnly(mutationLeases
            .DistinctBy(static lease => lease.ScopeKey)
            .OrderBy(static lease => lease.ScopeKey.Value, StringComparer.Ordinal)
            .ToArray());
        this.PersonalRankingShareMutationLease = personalRankingShareMutationLease;
        this.PersonalRankingCatalogMutationLease = personalRankingCatalogMutationLease;
        this.ShareSourceMutationLeases = Array.AsReadOnly(new[]
            {
                personalRankingShareMutationLease,
                personalRankingCatalogMutationLease,
            }
            .Where(static lease => lease is not null)
            .Select(static lease => lease!)
            .ToArray());
    }

    public IReadOnlyCollection<RatingRankingMutationLease> MutationLeases { get; }

    public ShareSourceMutationLease? PersonalRankingShareMutationLease { get; }

    public ShareSourceMutationLease? PersonalRankingCatalogMutationLease { get; }

    public IReadOnlyCollection<ShareSourceMutationLease> ShareSourceMutationLeases { get; }
}
