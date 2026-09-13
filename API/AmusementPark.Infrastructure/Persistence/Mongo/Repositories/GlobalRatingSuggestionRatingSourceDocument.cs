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

internal sealed class GlobalRatingSuggestionRatingSourceDocument
{
    public RatingTargetType TargetType { get; init; }

    public string TargetId { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public ParkItemCategory? ParkItemCategory { get; init; }

    public ParkItemType? ParkItemType { get; init; }

    public double Value { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}
