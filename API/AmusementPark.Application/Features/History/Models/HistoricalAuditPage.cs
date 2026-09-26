using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Models;

public sealed record HistoricalAuditPage(
    IReadOnlyCollection<HistoricalReviewEvent> Items,
    HistoricalAuditCursor? NextCursor);
