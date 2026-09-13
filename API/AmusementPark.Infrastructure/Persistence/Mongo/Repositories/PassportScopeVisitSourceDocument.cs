using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class PassportScopeVisitSourceDocument
{
    public string Id { get; init; } = string.Empty;
    public string ParkId { get; init; } = string.Empty;
    public VisitDateDocument Date { get; init; } = new VisitDateDocument();
    public byte? ParkAssessmentValueHalfSteps { get; init; }
    public long? ContentMutationFenceToken { get; init; }
    public long? ContentMutationFenceStableToken { get; init; }
    public bool ContentMutationFenceReady { get; init; }
}
