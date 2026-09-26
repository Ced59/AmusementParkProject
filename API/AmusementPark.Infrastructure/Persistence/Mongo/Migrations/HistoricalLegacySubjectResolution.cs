using AmusementPark.Core.Domain.History;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

public sealed record HistoricalLegacySubjectResolution(
    HistoricalSubjectType SubjectType,
    string SubjectId,
    string HistoricalLabel,
    HistoricalSubjectPublicationPolicy PublicationPolicy,
    string? AnomalyCode);
