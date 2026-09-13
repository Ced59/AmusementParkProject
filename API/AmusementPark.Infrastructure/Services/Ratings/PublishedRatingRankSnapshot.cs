using System.Collections.Concurrent;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Caching.Memory;

namespace AmusementPark.Infrastructure.Services.Ratings;

internal sealed record PublishedRatingRankSnapshot(
    long Generation,
    RatingPublishedRankingSnapshot Snapshot);
