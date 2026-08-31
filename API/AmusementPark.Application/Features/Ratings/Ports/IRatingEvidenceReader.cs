using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record RatingEvidenceTarget(
    RatingTargetType TargetType,
    string TargetId,
    string ParkId);

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
    IReadOnlyCollection<PublicParkItemEvidenceFact> PublicItems)
{
    public static readonly ParkRankingEvidenceFactsBatch Empty = new ParkRankingEvidenceFactsBatch(
        Array.Empty<ParkRankingContributorFacts>(),
        Array.Empty<PublicParkItemEvidenceFact>());
}

public interface IRatingEvidenceReader
{
    Task<ParkRankingEvidenceFactsBatch> ReadParkRankingFactsAsync(
        IReadOnlyCollection<RatingEvidenceTarget> targets,
        CancellationToken cancellationToken);
}
