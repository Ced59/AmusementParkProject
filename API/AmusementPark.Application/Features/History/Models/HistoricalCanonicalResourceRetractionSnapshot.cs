using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Models;

public sealed record HistoricalCanonicalResourceRetractionSnapshot(
    HistoricalFact? Fact,
    IReadOnlyCollection<HistoricalSourceReference> Sources);
