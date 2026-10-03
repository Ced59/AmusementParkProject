using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Models;

public sealed record HistoricalNarrativeCanonicalizationResult(
    Guid? CanonicalFactId,
    HistoricalNarrativeCanonicalizationState State,
    IReadOnlyCollection<string> Warnings);
