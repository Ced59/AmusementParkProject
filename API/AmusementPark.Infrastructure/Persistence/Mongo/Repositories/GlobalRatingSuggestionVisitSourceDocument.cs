using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class GlobalRatingSuggestionVisitSourceDocument
{
    public string Id { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public long? ContentMutationFenceToken { get; init; }

    public long? ContentMutationFenceStableToken { get; init; }

    public bool ContentMutationFenceReady { get; init; }

    public byte? AssessmentValueHalfSteps { get; init; }

    public DateTime? AssessmentUpdatedAtUtc { get; init; }
}
