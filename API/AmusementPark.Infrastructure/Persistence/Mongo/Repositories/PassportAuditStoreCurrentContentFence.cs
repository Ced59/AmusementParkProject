using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed record PassportAuditStoreCurrentContentFence(
    long? Token,
    bool IsReady,
    long? StableToken)
{
    public bool Matches(long? sourceFence)
    {
        return PassportAuditStore.ContentFenceAllowsAuditDelivery(
            this.Token,
            this.IsReady,
            this.StableToken,
            sourceFence);
    }
}
