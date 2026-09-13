using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class PassportItemVisitStatisticsSourceDocument
{
    public string Id { get; init; } = string.Empty;

    public VisitDateDocument Date { get; init; } = new VisitDateDocument();

    public long? ContentMutationFenceToken { get; init; }

    public long? ContentMutationFenceStableToken { get; init; }

    public bool ContentMutationFenceReady { get; init; }
}
