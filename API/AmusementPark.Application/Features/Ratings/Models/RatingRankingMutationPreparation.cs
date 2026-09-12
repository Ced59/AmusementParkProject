using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed class RatingRankingMutationPreparation
{
    public RatingRankingMutationPreparation(
        IReadOnlyCollection<RatingRankingMutationLease> mutationLeases,
        ShareSourceMutationLease? personalRankingShareMutationLease = null,
        ShareSourceMutationLease? personalRankingCatalogMutationLease = null,
        IReadOnlyCollection<ShareSourceMutationLease>? publicCatalogMutationLeases = null,
        IReadOnlyCollection<ShareSourceMutationLease>? personalRatingShareMutationLeases = null)
    {
        ArgumentNullException.ThrowIfNull(mutationLeases);
        this.MutationLeases = Array.AsReadOnly(mutationLeases
            .DistinctBy(static lease => lease.ScopeKey)
            .OrderBy(static lease => lease.ScopeKey.Value, StringComparer.Ordinal)
            .ToArray());
        this.PersonalRankingShareMutationLease = personalRankingShareMutationLease;
        this.PersonalRatingShareMutationLeases = Array.AsReadOnly(
            (personalRatingShareMutationLeases ?? Array.Empty<ShareSourceMutationLease>())
            .DistinctBy(static lease => lease.ScopeKey)
            .OrderBy(static lease => lease.ScopeKey, StringComparer.Ordinal)
            .ToArray());
        this.PersonalRankingCatalogMutationLease = personalRankingCatalogMutationLease;
        this.PublicCatalogMutationLeases = Array.AsReadOnly(
            (publicCatalogMutationLeases ?? Array.Empty<ShareSourceMutationLease>())
            .DistinctBy(static lease => lease.ScopeKey)
            .OrderBy(static lease => lease.ScopeKey, StringComparer.Ordinal)
            .ToArray());
        this.ShareSourceMutationLeases = Array.AsReadOnly(new[]
            {
                personalRankingShareMutationLease,
                personalRankingCatalogMutationLease,
            }
            .Where(static lease => lease is not null)
            .Select(static lease => lease!)
            .Concat(this.PersonalRatingShareMutationLeases)
            .Concat(this.PublicCatalogMutationLeases)
            .ToArray());
    }

    public IReadOnlyCollection<RatingRankingMutationLease> MutationLeases { get; }

    public ShareSourceMutationLease? PersonalRankingShareMutationLease { get; }

    public IReadOnlyCollection<ShareSourceMutationLease> PersonalRatingShareMutationLeases { get; }

    public ShareSourceMutationLease? PersonalRankingCatalogMutationLease { get; }

    public IReadOnlyCollection<ShareSourceMutationLease> PublicCatalogMutationLeases { get; }

    public IReadOnlyCollection<ShareSourceMutationLease> ShareSourceMutationLeases { get; }
}
