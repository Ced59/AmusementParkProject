using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalSourceRepository
{
    Task<HistoricalRevisionWriteDisposition> AppendRevisionAsync(
        HistoricalSourceReference source,
        HistoricalReviewEvent transitionReviewEvent,
        CancellationToken cancellationToken);

    Task<HistoricalSourceReference?> GetRevisionAsync(
        Guid sourceId,
        int revision,
        CancellationToken cancellationToken);

    Task<HistoricalSourceReference?> GetLatestRevisionAsync(
        Guid sourceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoricalSourceReference>> GetRevisionsAsync(
        IReadOnlyCollection<HistoricalSourceRevisionReference> sourceReferences,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoricalSourceReference>> GetLatestRevisionsAsync(
        IReadOnlyCollection<Guid> sourceIds,
        CancellationToken cancellationToken);
}
