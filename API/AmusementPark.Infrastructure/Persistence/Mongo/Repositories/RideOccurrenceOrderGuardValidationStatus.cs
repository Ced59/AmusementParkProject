using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal enum RideOccurrenceOrderGuardValidationStatus
{
    Validated = 1,
    Stale = 2,
    Unavailable = 3,
}
