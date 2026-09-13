using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed record UserRideOccurrenceRepositoryCurrentContentFence(
    bool VisitExists,
    bool IsEnforced,
    long? Token,
    long? StableToken = null)
{
    public bool Matches(long? token)
    {
        if (!this.Token.HasValue)
        {
            return !token.HasValue;
        }

        if (this.IsEnforced)
        {
            return token == this.Token;
        }

        return this.StableToken.HasValue
            ? token >= this.StableToken && token <= this.Token
            : !token.HasValue || token is >= 1 && token <= this.Token;
    }
}
