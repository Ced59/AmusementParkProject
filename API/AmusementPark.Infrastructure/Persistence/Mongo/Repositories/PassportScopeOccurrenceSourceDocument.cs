using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class PassportScopeOccurrenceSourceDocument
{
    public string Id { get; init; } = string.Empty;
    public string VisitId { get; init; } = string.Empty;
    public string ParkId { get; init; } = string.Empty;
    public string ParkItemId { get; init; } = string.Empty;
    public RideOccurrenceStatus Status { get; init; }
    public byte? AssessmentValueHalfSteps { get; init; }
    public string? HistoricalCategory { get; init; }
    public long? ContentMutationFenceToken { get; init; }
}
