using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed record PublicHistoricalTimelineEntryResult(
    HistoricalFact Fact,
    IReadOnlyCollection<HistoricalSourceReference> Sources);
