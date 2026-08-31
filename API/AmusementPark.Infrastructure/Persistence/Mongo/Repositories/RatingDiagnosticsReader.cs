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

public sealed class RatingDiagnosticsReader : IRatingDiagnosticsReader
{
    internal const int DistinctValueSampleLimit = 25;

    internal const string UserRatingsTargetIndexName = "idx_user_ratings_target";

    internal const string RatingAggregatesTargetIndexName = "idx_rating_aggregates_target_unique";

    private static readonly TimeSpan QueryMaxTime = TimeSpan.FromSeconds(30);

    private readonly IMongoCollection<BsonDocument> userRatingsCollection;
    private readonly IMongoCollection<BsonDocument> ratingAggregatesCollection;
    private readonly IMongoCollection<ParkDocument> parksCollection;
    private readonly IMongoCollection<ParkItemDocument> parkItemsCollection;
    private readonly MongoDbSettings settings;

    public RatingDiagnosticsReader(IMongoDatabase database, MongoDbSettings settings)
    {
        this.settings = settings;
        this.userRatingsCollection = database.GetCollection<BsonDocument>(settings.UserRatingsCollectionName);
        this.ratingAggregatesCollection = database.GetCollection<BsonDocument>(settings.RatingAggregatesCollectionName);
        this.parksCollection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.parkItemsCollection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
    }

    public async Task<RatingDiagnosticsResult> GetDiagnosticsAsync(CancellationToken cancellationToken)
    {
        long startedAt = Stopwatch.GetTimestamp();
        RatingIndexAssessment indexAssessment = await ReadIndexAssessmentAsync(cancellationToken);
        IReadOnlyCollection<RatingIndexStatusResult> indexes = indexAssessment.Statuses;
        bool canEvaluateSourceIntegrity = indexAssessment.RatingAggregatesTargetLookupSupported;
        bool canEvaluateOrphans = indexAssessment.UserRatingsTargetLookupSupported;
        EligibleTargetInventory eligibleTargets = await ReadEligibleTargetInventoryAsync(cancellationToken);
        BsonDocument facets = await RunPipelineAsync(
            this.userRatingsCollection,
            BuildUserRatingsDiagnosticPipeline(
                this.settings.RatingAggregatesCollectionName,
                eligibleTargets.ParkIds,
                eligibleTargets.ParkItemIds,
                canEvaluateSourceIntegrity),
            cancellationToken);
        long orphanAggregateCount = canEvaluateOrphans
            ? await ReadOrphanAggregateCountAsync(cancellationToken)
            : 0;

        BsonDocument summary = GetFirstFacetDocument(facets, "summary");
        BsonDocument duplicates = GetFirstFacetDocument(facets, "duplicates");
        BsonDocument distinctValues = GetFirstFacetDocument(facets, "distinctValues");
        IReadOnlyCollection<string> distinctValueSample = ReadDistinctValueSample(distinctValues);
        long distinctValueCount = ReadNestedCount(distinctValues, "count");

        RatingAnomalySummaryResult anomalies = new RatingAnomalySummaryResult(
            ReadInt64(summary, "nonNumericValueCount"),
            ReadInt64(summary, "unexpectedValueStorageTypeCount"),
            ReadInt64(summary, "outOfRangeValueCount"),
            ReadInt64(summary, "nonHalfStepValueCount"),
            ReadInt64(summary, "nearHalfStepValueCount"),
            ReadInt64(summary, "missingUserIdCount"),
            ReadInt64(summary, "missingTargetCount"),
            ReadInt64(duplicates, "duplicateVoteKeyCount"),
            ReadInt64(duplicates, "extraDuplicateDocumentCount"));
        RatingAggregateIntegrityResult aggregateIntegrity = EvaluateAggregateIntegrity(
            facets,
            canEvaluateSourceIntegrity,
            canEvaluateOrphans,
            orphanAggregateCount);

        return new RatingDiagnosticsResult(
            DateTime.UtcNow,
            (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
            ReadInt64(summary, "totalRatings"),
            distinctValueCount,
            distinctValueSample,
            distinctValueCount > distinctValueSample.Count,
            anomalies,
            aggregateIntegrity,
            CompleteTargetDistribution(
                ReadTargetDistribution(facets),
                eligibleTargets.ParkIds.Count,
                eligibleTargets.ParkItemIds.Count),
            indexes);
    }

    internal static IReadOnlyCollection<BsonDocument> BuildUserRatingsDiagnosticPipeline(
        string ratingAggregatesCollectionName,
        IReadOnlyCollection<string> eligibleParkIds,
        IReadOnlyCollection<string> eligibleParkItemIds,
        bool includeAggregateIntegrity)
    {
        BsonDocument facetStage = BsonDocument.Parse(
            """
            {
              "$facet": {
                "summary": [
                  {
                    "$group": {
                      "_id": null,
                      "totalRatings": { "$sum": 1 },
                      "nonNumericValueCount": { "$sum": { "$cond": [ { "$not": [ "$_diagnosticIsNumericValue" ] }, 1, 0 ] } },
                      "unexpectedValueStorageTypeCount": { "$sum": { "$cond": [ { "$ne": [ "$_diagnosticValueType", "double" ] }, 1, 0 ] } },
                      "outOfRangeValueCount": { "$sum": { "$cond": [ { "$and": [ "$_diagnosticIsNumericValue", { "$not": [ "$_diagnosticInRange" ] } ] }, 1, 0 ] } },
                      "nonHalfStepValueCount": { "$sum": { "$cond": [ { "$and": [ "$_diagnosticInRange", { "$not": [ "$_diagnosticIsExactHalfStep" ] } ] }, 1, 0 ] } },
                      "nearHalfStepValueCount": { "$sum": { "$cond": [ { "$and": [ "$_diagnosticInRange", { "$not": [ "$_diagnosticIsExactHalfStep" ] }, { "$gt": [ "$_diagnosticHalfStepDistance", 0 ] }, { "$lte": [ "$_diagnosticHalfStepDistance", 0.000001 ] } ] }, 1, 0 ] } },
                      "missingUserIdCount": { "$sum": { "$cond": [ { "$not": [ "$_diagnosticHasUser" ] }, 1, 0 ] } },
                      "missingTargetCount": { "$sum": { "$cond": [ { "$not": [ "$_diagnosticHasTarget" ] }, 1, 0 ] } }
                    }
                  },
                  { "$project": { "_id": 0 } }
                ],
                "distinctValues": [
                  { "$match": { "_diagnosticIsNumericValue": true } },
                  { "$group": { "_id": "$_diagnosticNumericValue" } },
                  { "$sort": { "_id": 1 } },
                  {
                    "$facet": {
                      "count": [ { "$count": "value" } ],
                      "sample": [
                        { "$limit": 25 },
                        { "$project": { "_id": 0, "value": "$_id" } }
                      ]
                    }
                  }
                ],
                "duplicates": [
                  { "$match": { "_diagnosticHasUser": true, "_diagnosticHasTarget": true } },
                  {
                    "$group": {
                      "_id": { "userId": "$_diagnosticUserText", "targetType": "$_diagnosticTargetType", "targetId": "$_diagnosticTargetText" },
                      "documentCount": { "$sum": 1 }
                    }
                  },
                  { "$match": { "documentCount": { "$gt": 1 } } },
                  {
                    "$group": {
                      "_id": null,
                      "duplicateVoteKeyCount": { "$sum": 1 },
                      "extraDuplicateDocumentCount": { "$sum": { "$subtract": [ "$documentCount", 1 ] } }
                    }
                  },
                  { "$project": { "_id": 0 } }
                ],
                "targetDistribution": [
                  { "$match": { "_diagnosticHasUser": true, "_diagnosticHasTarget": true } },
                  {
                    "$group": {
                      "_id": { "targetType": "$_diagnosticTargetType", "targetId": "$_diagnosticTargetText", "userId": "$_diagnosticUserText" },
                      "observationCount": { "$sum": 1 }
                    }
                  },
                  {
                    "$group": {
                      "_id": { "targetType": "$_id.targetType", "targetId": "$_id.targetId" },
                      "uniqueContributorCount": { "$sum": 1 },
                      "ratingObservationCount": { "$sum": "$observationCount" }
                    }
                  },
                  {
                    "$set": {
                      "evidenceBand": {
                        "$switch": {
                          "branches": [
                            { "case": { "$lte": [ "$uniqueContributorCount", 2 ] }, "then": "1-2" },
                            { "case": { "$lte": [ "$uniqueContributorCount", 9 ] }, "then": "3-9" },
                            { "case": { "$lte": [ "$uniqueContributorCount", 29 ] }, "then": "10-29" },
                            { "case": { "$lte": [ "$uniqueContributorCount", 99 ] }, "then": "30-99" }
                          ],
                          "default": "100+"
                        }
                      }
                    }
                  },
                  {
                    "$group": {
                      "_id": { "targetType": "$_id.targetType", "evidenceBand": "$evidenceBand" },
                      "targetCount": { "$sum": 1 },
                      "ratingObservationCount": { "$sum": "$ratingObservationCount" },
                      "uniqueContributorCount": { "$sum": "$uniqueContributorCount" }
                    }
                  },
                  { "$sort": { "_id.targetType": 1, "_id.evidenceBand": 1 } }
                ],
                "integrity": [
                  { "$match": { "_diagnosticHasTarget": true } },
                  {
                    "$group": {
                      "_id": { "targetType": "$_diagnosticTargetType", "targetId": "$_diagnosticTargetText" },
                      "sourceRatingObservationCount": { "$sum": 1 },
                      "sourceContributorIds": { "$addToSet": { "$cond": [ "$_diagnosticHasUser", "$_diagnosticUserText", null ] } },
                      "sourceRatingSum": { "$sum": "$_diagnosticNumericValue" }
                    }
                  },
                  {
                    "$set": {
                      "sourceUniqueContributorCount": { "$size": { "$setDifference": [ "$sourceContributorIds", [ null ] ] } }
                    }
                  },
                  {
                    "$lookup": {
                      "from": "__ratingAggregates__",
                      "let": { "targetType": "$_id.targetType", "targetId": "$_id.targetId" },
                      "pipeline": [
                        { "$match": { "$expr": { "$and": [ { "$eq": [ "$targetType", "$$targetType" ] }, { "$eq": [ "$targetId", "$$targetId" ] } ] } } },
                        { "$limit": 2 }
                      ],
                      "as": "aggregates"
                    }
                  },
                  {
                    "$set": {
                      "aggregateCount": { "$size": "$aggregates" },
                      "aggregate": { "$arrayElemAt": [ "$aggregates", 0 ] }
                    }
                  },
                  {
                    "$project": {
                      "sourceRatingObservationCount": 1,
                      "sourceUniqueContributorCount": 1,
                      "sourceRatingSum": 1,
                      "aggregateCount": 1,
                      "aggregate.ratingCount": 1,
                      "aggregate.uniqueContributorCount": 1,
                      "aggregate.ratingSum": 1,
                      "aggregate.averageRating": 1,
                      "aggregate.bayesianScore": 1
                    }
                  }
                ]
              }
            }
            """);
        BsonDocument facets = facetStage["$facet"].AsBsonDocument;
        if (includeAggregateIntegrity)
        {
            facets["integrity"].AsBsonArray[3].AsBsonDocument["$lookup"].AsBsonDocument["from"] =
                ratingAggregatesCollectionName;
        }
        else
        {
            facets.Remove("integrity");
        }
        facets["targetDistribution"].AsBsonArray[0] =
            BuildEligibleTargetMatch(eligibleParkIds, eligibleParkItemIds);
        BsonDocument exactRatingValueStage = BsonDocument.Parse(
            """
            {
              "$set": {
                "_diagnosticIsExactHalfStep": false,
                "_diagnosticHalfStepDistance": { "$cond": [ "$_diagnosticInRange", { "$abs": { "$subtract": [ "$_diagnosticNumericValue", { "$divide": [ { "$round": [ { "$multiply": [ "$_diagnosticNumericValue", 2 ] }, 0 ] }, 2 ] } ] } }, null ] }
              }
            }
            """);
        exactRatingValueStage["$set"]["_diagnosticIsExactHalfStep"] =
            RatingValueMongoExpressions.BuildIsExactValidRatingValue("$value");

        return new[]
        {
            BsonDocument.Parse(
                """
                {
                  "$set": {
                    "_diagnosticIsNumericValue": { "$isNumber": "$value" },
                    "_diagnosticNumericValue": { "$cond": [ { "$isNumber": "$value" }, "$value", null ] },
                    "_diagnosticValueType": { "$type": "$value" },
                    "_diagnosticUserText": { "$cond": [ { "$eq": [ { "$type": "$userId" }, "string" ] }, "$userId", "" ] },
                    "_diagnosticTargetText": { "$cond": [ { "$eq": [ { "$type": "$targetId" }, "string" ] }, "$targetId", "" ] },
                    "_diagnosticTargetType": { "$cond": [ { "$eq": [ { "$type": "$targetType" }, "string" ] }, "$targetType", "Unknown" ] }
                  }
                }
                """),
            BsonDocument.Parse(
                """
                {
                  "$set": {
                    "_diagnosticHasUser": { "$gt": [ { "$strLenCP": { "$trim": { "input": "$_diagnosticUserText" } } }, 0 ] },
                    "_diagnosticHasTarget": { "$and": [ { "$gt": [ { "$strLenCP": { "$trim": { "input": "$_diagnosticTargetText" } } }, 0 ] }, { "$in": [ "$_diagnosticTargetType", [ "Park", "ParkItem" ] ] } ] },
                    "_diagnosticInRange": { "$and": [ "$_diagnosticIsNumericValue", { "$gte": [ "$_diagnosticNumericValue", 0.5 ] }, { "$lte": [ "$_diagnosticNumericValue", 5.0 ] } ] }
                  }
                }
                """),
            exactRatingValueStage,
            facetStage,
        };
    }

    internal static IReadOnlyCollection<RatingTargetDistributionResult> CompleteTargetDistribution(
        IReadOnlyCollection<RatingTargetDistributionResult> ratedTargets,
        long eligibleParkCount,
        long eligibleParkItemCount)
    {
        List<RatingTargetDistributionResult> results = new List<RatingTargetDistributionResult>();
        AddTargetTypeDistribution(results, ratedTargets, RatingTargetType.Park.ToString(), eligibleParkCount);
        AddTargetTypeDistribution(results, ratedTargets, RatingTargetType.ParkItem.ToString(), eligibleParkItemCount);
        return results;
    }

    internal static RatingAggregateIntegrityResult EvaluateAggregateIntegrity(
        BsonDocument facets,
        bool isSourceComparisonEvaluated,
        bool isOrphanCheckEvaluated,
        long orphanAggregateCount)
    {
        if (!isSourceComparisonEvaluated
            || !facets.TryGetValue("integrity", out BsonValue? value)
            || !value.IsBsonArray)
        {
            return new RatingAggregateIntegrityResult(
                isSourceComparisonEvaluated,
                isOrphanCheckEvaluated,
                0,
                0,
                0,
                0,
                0,
                orphanAggregateCount);
        }

        long sourceTargetCount = 0;
        long missingAggregateCount = 0;
        long divergentAggregateCount = 0;
        long contributorCountMismatchCount = 0;
        long derivedScoreMismatchCount = 0;
        foreach (BsonValue item in value.AsBsonArray)
        {
            if (!item.IsBsonDocument)
            {
                continue;
            }

            sourceTargetCount++;
            BsonDocument document = item.AsBsonDocument;
            long aggregateCount = ReadInt64(document, "aggregateCount");
            if (aggregateCount == 0)
            {
                missingAggregateCount++;
                continue;
            }

            if (aggregateCount != 1
                || !document.TryGetValue("aggregate", out BsonValue? aggregateValue)
                || !aggregateValue.IsBsonDocument)
            {
                divergentAggregateCount++;
                continue;
            }

            BsonDocument aggregate = aggregateValue.AsBsonDocument;
            long sourceObservationCount = ReadInt64(document, "sourceRatingObservationCount");
            long sourceUniqueContributorCount = ReadInt64(document, "sourceUniqueContributorCount");
            double sourceRatingSum = ReadDouble(document, "sourceRatingSum");
            bool hasAggregateRatingCount = TryReadInt64(aggregate, "ratingCount", out long aggregateRatingCount);
            bool hasAggregateUniqueContributorCount = TryReadInt64(
                aggregate,
                "uniqueContributorCount",
                out long aggregateUniqueContributorCount);
            bool hasAggregateRatingSum = TryReadDouble(aggregate, "ratingSum", out double aggregateRatingSum);
            bool hasAggregateAverage = TryReadDouble(aggregate, "averageRating", out double aggregateAverage);
            bool hasAggregateBayesianScore = TryReadDouble(
                aggregate,
                "bayesianScore",
                out double aggregateBayesianScore);
            bool contributorCountMismatch = !hasAggregateUniqueContributorCount
                || sourceUniqueContributorCount != aggregateUniqueContributorCount;
            bool sourceProjectionMismatch = !hasAggregateRatingCount
                || !hasAggregateRatingSum
                || sourceObservationCount != aggregateRatingCount
                || !sourceRatingSum.Equals(aggregateRatingSum);
            double expectedAverage = RatingScoreCalculator.CalculateAverage(
                sourceRatingSum,
                sourceObservationCount);
            double expectedBayesianScore = RatingScoreCalculator.CalculateBayesianScore(
                sourceRatingSum,
                sourceObservationCount);
            bool derivedScoreMismatch = !hasAggregateAverage
                || !hasAggregateBayesianScore
                || !double.IsFinite(expectedAverage)
                || !double.IsFinite(expectedBayesianScore)
                || !expectedAverage.Equals(aggregateAverage)
                || !expectedBayesianScore.Equals(aggregateBayesianScore);
            bool sourceProjectionIsValid = RatingAggregate.HasValidSourceProjection(
                aggregateRatingCount,
                hasAggregateUniqueContributorCount ? aggregateUniqueContributorCount : null,
                aggregateRatingSum,
                aggregateAverage,
                aggregateBayesianScore,
                sourceObservationCount,
                sourceUniqueContributorCount,
                sourceRatingSum);

            if (contributorCountMismatch)
            {
                contributorCountMismatchCount++;
            }

            if (derivedScoreMismatch)
            {
                derivedScoreMismatchCount++;
            }

            if (contributorCountMismatch
                || sourceProjectionMismatch
                || derivedScoreMismatch
                || !sourceProjectionIsValid)
            {
                divergentAggregateCount++;
            }
        }

        return new RatingAggregateIntegrityResult(
            isSourceComparisonEvaluated,
            isOrphanCheckEvaluated,
            sourceTargetCount,
            missingAggregateCount,
            divergentAggregateCount,
            contributorCountMismatchCount,
            derivedScoreMismatchCount,
            orphanAggregateCount);
    }

    private static BsonDocument BuildEligibleTargetMatch(
        IReadOnlyCollection<string> eligibleParkIds,
        IReadOnlyCollection<string> eligibleParkItemIds)
    {
        BsonArray parkIds = new BsonArray(eligibleParkIds.Select(static id => new BsonString(id)));
        BsonArray parkItemIds = new BsonArray(eligibleParkItemIds.Select(static id => new BsonString(id)));
        return new BsonDocument("$match", new BsonDocument
        {
            { "_diagnosticHasUser", true },
            { "_diagnosticHasTarget", true },
            { "_diagnosticIsExactHalfStep", true },
            {
                "$or",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "_diagnosticTargetType", RatingTargetType.Park.ToString() },
                        { "_diagnosticTargetText", new BsonDocument("$in", parkIds) },
                    },
                    new BsonDocument
                    {
                        { "_diagnosticTargetType", RatingTargetType.ParkItem.ToString() },
                        { "_diagnosticTargetText", new BsonDocument("$in", parkItemIds) },
                    },
                }
            },
        });
    }

    private static void AddTargetTypeDistribution(
        ICollection<RatingTargetDistributionResult> results,
        IReadOnlyCollection<RatingTargetDistributionResult> ratedTargets,
        string targetType,
        long eligibleTargetCount)
    {
        List<RatingTargetDistributionResult> targetTypeResults = ratedTargets
            .Where(result => string.Equals(result.TargetType, targetType, StringComparison.Ordinal)
                && !string.Equals(result.EvidenceBand, "0", StringComparison.Ordinal))
            .OrderBy(static result => GetEvidenceBandOrder(result.EvidenceBand))
            .ToList();
        long ratedTargetCount = targetTypeResults.Sum(static result => result.TargetCount);
        results.Add(new RatingTargetDistributionResult(
            targetType,
            "0",
            Math.Max(0, eligibleTargetCount - ratedTargetCount),
            0,
            0));

        foreach (RatingTargetDistributionResult result in targetTypeResults)
        {
            results.Add(result);
        }
    }

    private static int GetEvidenceBandOrder(string evidenceBand)
    {
        return evidenceBand switch
        {
            "1-2" => 1,
            "3-9" => 2,
            "10-29" => 3,
            "30-99" => 4,
            "100+" => 5,
            _ => int.MaxValue,
        };
    }

    internal static IReadOnlyCollection<RatingIndexStatusResult> EvaluateIndexStatuses(
        string userRatingsCollectionName,
        IReadOnlyCollection<BsonDocument> userRatingIndexes,
        string ratingAggregatesCollectionName,
        IReadOnlyCollection<BsonDocument> ratingAggregateIndexes)
    {
        List<ExpectedIndex> expectedIndexes = new List<ExpectedIndex>
        {
            new ExpectedIndex(userRatingsCollectionName, "idx_user_ratings_user_target_unique", true, new BsonDocument { { "userId", 1 }, { "targetType", 1 }, { "targetId", 1 } }),
            new ExpectedIndex(userRatingsCollectionName, UserRatingsTargetIndexName, false, new BsonDocument { { "targetType", 1 }, { "targetId", 1 } }),
            new ExpectedIndex(userRatingsCollectionName, "idx_user_ratings_user_updated", false, new BsonDocument { { "userId", 1 }, { "updatedAt", -1 } }),
            new ExpectedIndex(userRatingsCollectionName, "idx_user_ratings_user_park", false, new BsonDocument { { "userId", 1 }, { "parkId", 1 } }),
            new ExpectedIndex(ratingAggregatesCollectionName, RatingAggregatesTargetIndexName, true, new BsonDocument { { "targetType", 1 }, { "targetId", 1 } }),
            new ExpectedIndex(ratingAggregatesCollectionName, "idx_rating_aggregates_ranking", false, new BsonDocument { { "bayesianScore", -1 }, { "ratingCount", -1 }, { "averageRating", -1 } }),
            new ExpectedIndex(ratingAggregatesCollectionName, "idx_rating_aggregates_type_ranking", false, new BsonDocument { { "targetType", 1 }, { "bayesianScore", -1 }, { "ratingCount", -1 } }),
            new ExpectedIndex(ratingAggregatesCollectionName, "idx_rating_aggregates_category_ranking", false, new BsonDocument { { "parkItemCategory", 1 }, { "bayesianScore", -1 }, { "ratingCount", -1 } }),
        };
        List<RatingIndexStatusResult> results = new List<RatingIndexStatusResult>();

        foreach (ExpectedIndex expectedIndex in expectedIndexes)
        {
            IReadOnlyCollection<BsonDocument> source = string.Equals(
                expectedIndex.Collection,
                userRatingsCollectionName,
                StringComparison.Ordinal)
                ? userRatingIndexes
                : ratingAggregateIndexes;
            BsonDocument? actual = source.FirstOrDefault(index =>
                index.TryGetValue("name", out BsonValue? name)
                && name.IsString
                && string.Equals(name.AsString, expectedIndex.Name, StringComparison.Ordinal));
            bool isPresent = actual is not null;
            bool isUnique = isPresent
                && actual!.TryGetValue("unique", out BsonValue? unique)
                && unique.IsBoolean
                && unique.AsBoolean;
            bool isHidden = isPresent
                && actual!.TryGetValue("hidden", out BsonValue? hidden)
                && hidden.IsBoolean
                && hidden.AsBoolean;
            bool hasUnexpectedOptions = isPresent && HasUnexpectedIndexOptions(actual!);
            BsonDocument? actualKeys = isPresent
                && actual!.TryGetValue("key", out BsonValue? key)
                && key.IsBsonDocument
                    ? key.AsBsonDocument
                    : null;
            bool supportsExpectedQueries = isPresent
                && !isHidden
                && actualKeys is not null
                && SupportsExpectedIndexQueries(expectedIndex, actualKeys)
                && !HasQueryLimitingIndexOptions(actual!);
            bool matchesExpectedDefinition = isPresent
                && isUnique == expectedIndex.IsUnique
                && !isHidden
                && !hasUnexpectedOptions
                && actualKeys is not null
                && expectedIndex.Keys.Equals(actualKeys);

            results.Add(new RatingIndexStatusResult(
                expectedIndex.Collection,
                expectedIndex.Name,
                isPresent,
                isUnique,
                isHidden,
                hasUnexpectedOptions,
                supportsExpectedQueries,
                matchesExpectedDefinition,
                expectedIndex.Keys.ToJson(),
                actualKeys?.ToJson()));
        }

        return results;
    }

    internal static bool HasQueryCompatibleTargetIndex(IEnumerable<BsonDocument> indexes)
    {
        return indexes.Any(index =>
            !IsHiddenIndex(index)
            && !HasQueryLimitingIndexOptions(index)
            && index.TryGetValue("key", out BsonValue? key)
            && key.IsBsonDocument
            && HasLeadingTargetEqualityKeys(key.AsBsonDocument));
    }

    private static bool SupportsExpectedIndexQueries(ExpectedIndex expectedIndex, BsonDocument actualKeys)
    {
        return IsTargetLookupIndex(expectedIndex)
            ? HasLeadingTargetEqualityKeys(actualKeys)
            : IndexKeysStartWith(actualKeys, expectedIndex.Keys);
    }

    private static bool IsTargetLookupIndex(ExpectedIndex expectedIndex)
    {
        return string.Equals(expectedIndex.Name, UserRatingsTargetIndexName, StringComparison.Ordinal)
            || string.Equals(expectedIndex.Name, RatingAggregatesTargetIndexName, StringComparison.Ordinal);
    }

    private static bool HasLeadingTargetEqualityKeys(BsonDocument actualKeys)
    {
        if (actualKeys.ElementCount < 2)
        {
            return false;
        }

        BsonElement first = actualKeys.GetElement(0);
        BsonElement second = actualKeys.GetElement(1);
        bool hasExpectedNames = (string.Equals(first.Name, "targetType", StringComparison.Ordinal)
                && string.Equals(second.Name, "targetId", StringComparison.Ordinal))
            || (string.Equals(first.Name, "targetId", StringComparison.Ordinal)
                && string.Equals(second.Name, "targetType", StringComparison.Ordinal));
        return hasExpectedNames
            && IsAscendingOrDescending(first.Value)
            && IsAscendingOrDescending(second.Value);
    }

    private static bool IsAscendingOrDescending(BsonValue value)
    {
        return value.IsNumeric && Math.Abs(value.ToInt32()) == 1;
    }

    private static bool IndexKeysStartWith(BsonDocument actualKeys, BsonDocument expectedPrefix)
    {
        if (actualKeys.ElementCount < expectedPrefix.ElementCount)
        {
            return false;
        }

        for (int index = 0; index < expectedPrefix.ElementCount; index++)
        {
            BsonElement actualElement = actualKeys.GetElement(index);
            BsonElement expectedElement = expectedPrefix.GetElement(index);
            if (!string.Equals(actualElement.Name, expectedElement.Name, StringComparison.Ordinal)
                || !actualElement.Value.Equals(expectedElement.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsHiddenIndex(BsonDocument index)
    {
        return index.TryGetValue("hidden", out BsonValue? hidden)
            && hidden.IsBoolean
            && hidden.AsBoolean;
    }

    private static bool HasUnexpectedIndexOptions(BsonDocument index)
    {
        return index.Contains("expireAfterSeconds") || HasQueryLimitingIndexOptions(index);
    }

    private static bool HasQueryLimitingIndexOptions(BsonDocument index)
    {
        return index.Contains("partialFilterExpression")
            || HasActiveOrInvalidSparseOption(index)
            || HasNonSimpleCollation(index);
    }

    private static bool HasActiveOrInvalidSparseOption(BsonDocument index)
    {
        return index.TryGetValue("sparse", out BsonValue? sparse)
            && (!sparse.IsBoolean || sparse.AsBoolean);
    }

    private static bool HasNonSimpleCollation(BsonDocument index)
    {
        if (!index.TryGetValue("collation", out BsonValue? collation))
        {
            return false;
        }

        if (!collation.IsBsonDocument)
        {
            return true;
        }

        BsonDocument definition = collation.AsBsonDocument;
        return definition.ElementCount != 1
            || !definition.TryGetValue("locale", out BsonValue? locale)
            || !locale.IsString
            || !string.Equals(locale.AsString, "simple", StringComparison.Ordinal);
    }

    private async Task<EligibleTargetInventory> ReadEligibleTargetInventoryAsync(CancellationToken cancellationToken)
    {
        FindOptions findOptions = new FindOptions
        {
            MaxTime = QueryMaxTime,
        };
        FilterDefinition<ParkDocument> visibleParkFilter = Builders<ParkDocument>.Filter.Eq(
            document => document.IsVisible,
            true);
        List<EligibleParkProjection> parks = await this.parksCollection.Find(visibleParkFilter, findOptions)
            .Project(static document => new EligibleParkProjection
            {
                Id = document.Id,
                Status = document.Status,
            })
            .ToListAsync(cancellationToken);
        HashSet<string> eligibleParkIds = parks
            .Where(static document => document.Status.CanReceiveVisitorRatings())
            .Select(static document => document.Id)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        FilterDefinition<ParkItemDocument> visibleParkItemFilter = Builders<ParkItemDocument>.Filter.Eq(
            document => document.IsVisible,
            true);
        List<EligibleParkItemProjection> parkItems = await this.parkItemsCollection.Find(visibleParkItemFilter, findOptions)
            .Project(static document => new EligibleParkItemProjection
            {
                Id = document.Id,
                ParkId = document.ParkId,
                Category = document.Category,
                AttractionStatus = document.AttractionDetails!.Status,
            })
            .ToListAsync(cancellationToken);
        HashSet<string> eligibleParkItemIds = parkItems
            .Where(document => eligibleParkIds.Contains(document.ParkId)
                && ParkItemStatusNormalizer.CanReceiveVisitorRatings(
                    document.Category,
                    document.AttractionStatus))
            .Select(static document => document.Id)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        return new EligibleTargetInventory(eligibleParkIds, eligibleParkItemIds);
    }

    private async Task<long> ReadOrphanAggregateCountAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<BsonDocument> pipeline = BuildOrphanAggregatePipeline(
            this.settings.UserRatingsCollectionName);
        BsonDocument result = await RunPipelineAsync(this.ratingAggregatesCollection, pipeline, cancellationToken);
        return ReadInt64(result, "value");
    }

    internal static IReadOnlyCollection<BsonDocument> BuildOrphanAggregatePipeline(
        string userRatingsCollectionName)
    {
        BsonDocument lookupStage = BsonDocument.Parse(
            """
            {
              "$lookup": {
                "from": "__userRatings__",
                "let": { "targetType": "$targetType", "targetId": "$targetId" },
                "pipeline": [
                  { "$match": { "$expr": { "$and": [ { "$eq": [ "$targetType", "$$targetType" ] }, { "$eq": [ "$targetId", "$$targetId" ] } ] } } },
                  { "$limit": 1 }
                ],
                "as": "_diagnosticSources"
              }
            }
            """);
        lookupStage["$lookup"].AsBsonDocument["from"] = userRatingsCollectionName;
        BsonDocument validEmptySnapshot = new BsonDocument
        {
            { "ratingCount", 0 },
            { "ratingSum", 0d },
            { "averageRating", 0d },
            { "bayesianScore", RatingScoreCalculator.PriorMean },
        };
        return new[]
        {
            new BsonDocument("$match", new BsonDocument("$nor", new BsonArray { validEmptySnapshot })),
            lookupStage,
            BsonDocument.Parse("{ \"$match\": { \"_diagnosticSources\": { \"$size\": 0 } } }"),
            BsonDocument.Parse("{ \"$count\": \"value\" }"),
        };
    }

    private async Task<RatingIndexAssessment> ReadIndexAssessmentAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<BsonDocument> userRatingIndexes = await ListIndexesAsync(
            this.userRatingsCollection,
            cancellationToken);
        IReadOnlyCollection<BsonDocument> ratingAggregateIndexes = await ListIndexesAsync(
            this.ratingAggregatesCollection,
            cancellationToken);
        return new RatingIndexAssessment(
            EvaluateIndexStatuses(
                this.settings.UserRatingsCollectionName,
                userRatingIndexes,
                this.settings.RatingAggregatesCollectionName,
                ratingAggregateIndexes),
            HasQueryCompatibleTargetIndex(userRatingIndexes),
            HasQueryCompatibleTargetIndex(ratingAggregateIndexes));
    }

    private static async Task<IReadOnlyCollection<BsonDocument>> ListIndexesAsync(
        IMongoCollection<BsonDocument> collection,
        CancellationToken cancellationToken)
    {
        using IAsyncCursor<BsonDocument> cursor = await collection.Indexes.ListAsync(cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    private static async Task<BsonDocument> RunPipelineAsync(
        IMongoCollection<BsonDocument> collection,
        IReadOnlyCollection<BsonDocument> stages,
        CancellationToken cancellationToken)
    {
        AggregateOptions options = new AggregateOptions
        {
            AllowDiskUse = true,
            MaxTime = QueryMaxTime,
        };
        IAggregateFluent<BsonDocument> aggregation = collection.Aggregate(options);
        foreach (BsonDocument stage in stages)
        {
            aggregation = aggregation.AppendStage<BsonDocument>(stage);
        }

        return await aggregation.FirstOrDefaultAsync(cancellationToken) ?? new BsonDocument();
    }

    private static IReadOnlyCollection<RatingTargetDistributionResult> ReadTargetDistribution(BsonDocument facets)
    {
        if (!facets.TryGetValue("targetDistribution", out BsonValue? value) || !value.IsBsonArray)
        {
            return Array.Empty<RatingTargetDistributionResult>();
        }

        List<RatingTargetDistributionResult> results = new List<RatingTargetDistributionResult>();
        foreach (BsonValue item in value.AsBsonArray)
        {
            if (!item.IsBsonDocument)
            {
                continue;
            }

            BsonDocument document = item.AsBsonDocument;
            BsonDocument key = document.GetValue("_id", new BsonDocument()).AsBsonDocument;
            results.Add(new RatingTargetDistributionResult(
                ReadString(key, "targetType", "Unknown"),
                ReadString(key, "evidenceBand", "Unknown"),
                ReadInt64(document, "targetCount"),
                ReadInt64(document, "ratingObservationCount"),
                ReadInt64(document, "uniqueContributorCount")));
        }

        return results;
    }

    private static IReadOnlyCollection<string> ReadDistinctValueSample(BsonDocument distinctValues)
    {
        if (!distinctValues.TryGetValue("sample", out BsonValue? sample) || !sample.IsBsonArray)
        {
            return Array.Empty<string>();
        }

        return sample.AsBsonArray
            .Where(static item => item.IsBsonDocument && item.AsBsonDocument.Contains("value"))
            .Select(static item => FormatNumericValue(item.AsBsonDocument["value"]))
            .ToList();
    }

    private static string FormatNumericValue(BsonValue value)
    {
        if (value.IsDouble)
        {
            double number = value.AsDouble;
            if (double.IsNaN(number))
            {
                return "NaN";
            }

            if (double.IsPositiveInfinity(number))
            {
                return "+Infinity";
            }

            if (double.IsNegativeInfinity(number))
            {
                return "-Infinity";
            }

            return number.ToString("0.#################", CultureInfo.InvariantCulture);
        }

        return value.ToString() ?? string.Empty;
    }

    private static BsonDocument GetFirstFacetDocument(BsonDocument facets, string name)
    {
        if (!facets.TryGetValue(name, out BsonValue? value)
            || !value.IsBsonArray
            || value.AsBsonArray.Count == 0)
        {
            return new BsonDocument();
        }

        BsonValue first = value.AsBsonArray[0];
        return first.IsBsonDocument ? first.AsBsonDocument : new BsonDocument();
    }

    private static long ReadNestedCount(BsonDocument document, string name)
    {
        if (!document.TryGetValue(name, out BsonValue? value)
            || !value.IsBsonArray
            || value.AsBsonArray.Count == 0
            || !value.AsBsonArray[0].IsBsonDocument)
        {
            return 0;
        }

        return ReadInt64(value.AsBsonArray[0].AsBsonDocument, "value");
    }

    private static long ReadInt64(BsonDocument document, string name)
    {
        if (!document.TryGetValue(name, out BsonValue? value) || !value.IsNumeric)
        {
            return 0;
        }

        return value.ToInt64();
    }

    private static bool TryReadInt64(BsonDocument document, string name, out long result)
    {
        if (!document.TryGetValue(name, out BsonValue? value)
            || (!value.IsInt32 && !value.IsInt64))
        {
            result = 0;
            return false;
        }

        result = value.ToInt64();
        return true;
    }

    private static double ReadDouble(BsonDocument document, string name)
    {
        if (!document.TryGetValue(name, out BsonValue? value) || !value.IsNumeric)
        {
            return double.NaN;
        }

        return value.ToDouble();
    }

    private static bool TryReadDouble(BsonDocument document, string name, out double result)
    {
        if (!document.TryGetValue(name, out BsonValue? value) || !value.IsNumeric)
        {
            result = 0d;
            return false;
        }

        result = value.ToDouble();
        return true;
    }

    private static string ReadString(BsonDocument document, string name, string fallback)
    {
        if (!document.TryGetValue(name, out BsonValue? value) || !value.IsString)
        {
            return fallback;
        }

        return value.AsString;
    }

    private sealed record ExpectedIndex(
        string Collection,
        string Name,
        bool IsUnique,
        BsonDocument Keys);

    private sealed record RatingIndexAssessment(
        IReadOnlyCollection<RatingIndexStatusResult> Statuses,
        bool UserRatingsTargetLookupSupported,
        bool RatingAggregatesTargetLookupSupported);

    private sealed record EligibleTargetInventory(
        IReadOnlySet<string> ParkIds,
        IReadOnlySet<string> ParkItemIds);

    private sealed class EligibleParkProjection
    {
        public string Id { get; init; } = string.Empty;

        public ParkStatus Status { get; init; }
    }

    private sealed class EligibleParkItemProjection
    {
        public string Id { get; init; } = string.Empty;

        public string ParkId { get; init; } = string.Empty;

        public ParkItemCategory Category { get; init; }

        public string? AttractionStatus { get; init; }
    }
}
