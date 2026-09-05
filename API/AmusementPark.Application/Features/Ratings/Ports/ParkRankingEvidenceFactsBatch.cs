namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record ParkRankingEvidenceFactsBatch(
    IReadOnlyCollection<ParkRankingContributorFacts> Contributors,
    IReadOnlyCollection<PublicParkItemEvidenceFact> PublicItems,
    IReadOnlyCollection<RatingAggregateSourceFact> AggregateSources,
    IReadOnlyCollection<string> IncompletePublicInventoryParkIds,
    bool AggregateSourceFactsWereRead)
{
    public ParkRankingEvidenceFactsBatch(
        IReadOnlyCollection<ParkRankingContributorFacts> contributors,
        IReadOnlyCollection<PublicParkItemEvidenceFact> publicItems)
        : this(
            contributors,
            publicItems,
            Array.Empty<RatingAggregateSourceFact>(),
            Array.Empty<string>(),
            false)
    {
    }

    public static readonly ParkRankingEvidenceFactsBatch Empty = new ParkRankingEvidenceFactsBatch(
        Array.Empty<ParkRankingContributorFacts>(),
        Array.Empty<PublicParkItemEvidenceFact>(),
        Array.Empty<RatingAggregateSourceFact>(),
        Array.Empty<string>(),
        false);
}
