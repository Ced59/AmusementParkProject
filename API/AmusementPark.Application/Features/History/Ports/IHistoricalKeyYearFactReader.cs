using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalKeyYearFactReader
{
    Task<IReadOnlyCollection<HistoricalFact>> GetLatestDecisionEligibleRevisionsAsync(
        int limit,
        CancellationToken cancellationToken);
}
