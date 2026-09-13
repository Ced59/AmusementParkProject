using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset now;

    public FixedTimeProvider(DateTime nowUtc)
    {
        this.now = new DateTimeOffset(nowUtc);
    }

    public override DateTimeOffset GetUtcNow()
    {
        return this.now;
    }
}
