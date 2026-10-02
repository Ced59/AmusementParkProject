using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalNarrativeCanonicalizer
{
    Task<HistoricalNarrativeCanonicalizationResult> CanonicalizeAsync(
        HistoryEvent historyEvent,
        CancellationToken cancellationToken);
}
