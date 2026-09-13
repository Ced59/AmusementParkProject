using System.Diagnostics;
using System.Globalization;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed record RatingIndexAssessment(
    IReadOnlyCollection<RatingIndexStatusResult> Statuses,
    bool UserRatingsTargetLookupSupported,
    bool RatingAggregatesTargetLookupSupported);
