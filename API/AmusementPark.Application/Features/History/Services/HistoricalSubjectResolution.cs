using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

internal sealed record HistoricalSubjectResolution(
    HistoricalSubjectType SubjectType,
    string SubjectId,
    string Label,
    HistoricalSubjectPublicationPolicy PublicationPolicy,
    string? ContextParkId);
