using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed record PublicHistoricalRelationResult(
    HistoricalRelation Relation,
    IReadOnlyCollection<HistoricalSourceReference> Sources);
