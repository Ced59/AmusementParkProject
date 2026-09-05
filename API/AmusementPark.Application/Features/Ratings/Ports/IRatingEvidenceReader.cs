namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingEvidenceReader
{
    Task<ParkRankingEvidenceFactsBatch> ReadParkRankingFactsAsync(
        IReadOnlyCollection<RatingEvidenceTarget> targets,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RatingAggregateSourceFact>> ReadAggregateSourceFactsAsync(
        IReadOnlyCollection<RatingAggregateSourceTarget> targets,
        CancellationToken cancellationToken);
}
