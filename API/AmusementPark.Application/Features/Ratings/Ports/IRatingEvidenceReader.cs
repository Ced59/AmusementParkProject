using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record RatingEvidenceTarget(
    RatingTargetType TargetType,
    string TargetId,
    string ParkId);

public sealed record RatingAggregateSourceTarget(
    RatingTargetType TargetType,
    string TargetId);

public sealed record RatingAggregateSourceFact(
    RatingTargetType TargetType,
    string TargetId,
    long UniqueContributorCount,
    long RatingObservationCount,
    double RatingSum);

public sealed record ParkRankingContributorFacts(
    string ParkId,
    long UniqueContributorCount,
    long RatingObservationCount,
    long DirectParkContributorCount,
    long ItemContributorCount);

public sealed record PublicParkItemEvidenceFact(
    string ParkId,
    string TargetId,
    ParkItemCategory Category);

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

public interface IRatingEvidenceReader
{
    Task<ParkRankingEvidenceFactsBatch> ReadParkRankingFactsAsync(
        IReadOnlyCollection<RatingEvidenceTarget> targets,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RatingAggregateSourceFact>> ReadAggregateSourceFactsAsync(
        IReadOnlyCollection<RatingAggregateSourceTarget> targets,
        CancellationToken cancellationToken);
}
