using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class PassportItemOccurrenceStatisticsSourceDocument
{
    public string Id { get; init; } = string.Empty;

    public string VisitId { get; init; } = string.Empty;

    public long SortPosition { get; init; }

    public byte? AssessmentValueHalfSteps { get; init; }

    public long? ContentMutationFenceToken { get; init; }
}
