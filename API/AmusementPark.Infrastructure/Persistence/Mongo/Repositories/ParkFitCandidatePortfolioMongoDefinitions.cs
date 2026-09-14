using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using MongoDB.Bson;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ParkFitCandidatePortfolioMongoDefinitions
{
    internal const string JoinedParkField = "park";

    internal static BsonDocument[] BuildActivePipeline(
        string parksCollectionName,
        string? countryCode,
        int maximumActiveCandidateCount)
    {
        if (string.IsNullOrWhiteSpace(parksCollectionName))
        {
            throw new ArgumentException(
                "The parks collection name is required.",
                nameof(parksCollectionName));
        }

        if (maximumActiveCandidateCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumActiveCandidateCount));
        }

        return
        [
            new BsonDocument("$match", new BsonDocument("state", new BsonDocument(
                "$in",
                new BsonArray
                {
                    ParkFitRecommendationState.Active.ToString(),
                    ParkFitRecommendationState.Suspended.ToString(),
                }))),
            new BsonDocument("$lookup", new BsonDocument
            {
                ["from"] = parksCollectionName.Trim(),
                ["localField"] = "_id",
                ["foreignField"] = "_id",
                ["as"] = JoinedParkField,
            }),
            new BsonDocument("$unwind", $"${JoinedParkField}"),
            new BsonDocument("$match", BuildParkCandidateFilter(countryCode, $"{JoinedParkField}.")),
            new BsonDocument("$facet", new BsonDocument
            {
                ["activeCandidates"] = new BsonArray
                {
                    new BsonDocument("$match", new BsonDocument(
                        "state",
                        ParkFitRecommendationState.Active.ToString())),
                    new BsonDocument("$sort", new BsonDocument
                    {
                        [$"{JoinedParkField}.name"] = 1,
                        ["_id"] = 1,
                    }),
                    new BsonDocument("$limit", maximumActiveCandidateCount),
                    new BsonDocument("$replaceRoot", new BsonDocument(
                        "newRoot",
                        $"${JoinedParkField}")),
                },
                ["stateCounts"] = new BsonArray
                {
                    new BsonDocument("$group", new BsonDocument
                    {
                        ["_id"] = "$state",
                        ["count"] = new BsonDocument("$sum", 1),
                    }),
                },
            }),
        ];
    }

    internal static BsonDocument BuildParkCandidateFilter(string? countryCode)
    {
        return BuildParkCandidateFilter(countryCode, string.Empty);
    }

    private static BsonDocument BuildParkCandidateFilter(string? countryCode, string prefix)
    {
        BsonArray conditions =
        [
            new BsonDocument($"{prefix}isVisible", true),
            new BsonDocument($"{prefix}status", new BsonDocument(
                "$ne",
                ParkStatus.ClosedDefinitively.ToString())),
            new BsonDocument($"{prefix}latitude", new BsonDocument
            {
                ["$exists"] = true,
                ["$ne"] = BsonNull.Value,
            }),
            new BsonDocument($"{prefix}longitude", new BsonDocument
            {
                ["$exists"] = true,
                ["$ne"] = BsonNull.Value,
            }),
            new BsonDocument("$or", new BsonArray
            {
                new BsonDocument($"{prefix}latitude", new BsonDocument("$ne", 0d)),
                new BsonDocument($"{prefix}longitude", new BsonDocument("$ne", 0d)),
            }),
        ];
        string normalizedCountryCode = (countryCode ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCountryCode.Length > 0)
        {
            conditions.Add(new BsonDocument($"{prefix}countryCode", normalizedCountryCode));
        }

        return new BsonDocument("$and", conditions);
    }
}
