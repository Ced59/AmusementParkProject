using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Contracts;

public sealed record HistoricalEvidenceSourceInput(
    Guid SourceId,
    int Revision,
    HistoricalEvidencePosition Position);
