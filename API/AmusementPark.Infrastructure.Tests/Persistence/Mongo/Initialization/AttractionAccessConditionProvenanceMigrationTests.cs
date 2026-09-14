using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class AttractionAccessConditionProvenanceMigrationTests
{
    [Fact]
    public void BuildFilter_ShouldTargetOnlyEmbeddedConditionsWithoutCurrentSchema()
    {
        BsonDocument filter = AttractionAccessConditionProvenanceMigration.BuildFilter();
        BsonDocument elementMatch = filter["attractionDetails.accessConditions"].AsBsonDocument["$elemMatch"].AsBsonDocument;
        BsonArray alternatives = elementMatch["$or"].AsBsonArray;
        BsonDocument existenceOperator = alternatives[0].AsBsonDocument["provenanceSchemaVersion"].AsBsonDocument;
        BsonDocument maximumVersionOperator = alternatives[2].AsBsonDocument["provenanceSchemaVersion"].AsBsonDocument;

        Assert.Equal(3, alternatives.Count);
        Assert.False(existenceOperator["$exists"].AsBoolean);
        Assert.True(alternatives[1].AsBsonDocument["provenanceSchemaVersion"].IsBsonNull);
        Assert.Equal(
            AttractionAccessCondition.CurrentProvenanceSchemaVersion,
            maximumVersionOperator["$lt"]);
    }

    [Fact]
    public void BuildLegacyDefaults_ShouldMarkUnknownEvidenceWithoutInventingAProof()
    {
        BsonDocument defaults = AttractionAccessConditionProvenanceMigration.BuildLegacyDefaults();

        Assert.Equal(AttractionAccessCondition.CurrentProvenanceSchemaVersion, defaults["provenanceSchemaVersion"]);
        Assert.Equal(AttractionAccessConditionSourceKind.Unknown.ToString(), defaults["sourceKind"]);
        Assert.Equal(AttractionAccessConditionConfidence.Unknown.ToString(), defaults["sourceConfidence"]);
        Assert.Equal(AttractionAccessConditionScope.Attraction.ToString(), defaults["scope"]);
        Assert.Empty(defaults["sourceSummary"].AsBsonArray);
        Assert.False(defaults.Contains("sourceUrl"));
        Assert.False(defaults.Contains("sourceReference"));
        Assert.False(defaults.Contains("collectedAtUtc"));
        Assert.False(defaults.Contains("verifiedAtUtc"));
    }

    [Fact]
    public void BuildPipeline_ShouldMigrateOnlyOldConditionsAndForceTheCurrentSchema()
    {
        BsonDocument stage = Assert.Single(AttractionAccessConditionProvenanceMigration.BuildPipeline());
        BsonArray conditionOperands = stage["$set"].AsBsonDocument["attractionDetails.accessConditions"]
            .AsBsonDocument["$map"].AsBsonDocument["in"].AsBsonDocument["$cond"].AsBsonArray;
        BsonArray mergeOperands = conditionOperands[1].AsBsonDocument["$mergeObjects"].AsBsonArray;

        Assert.Equal(3, conditionOperands.Count);
        Assert.Equal("$$condition", conditionOperands[2].AsString);
        Assert.Equal(3, mergeOperands.Count);
        Assert.IsType<BsonDocument>(mergeOperands[0]);
        Assert.Equal("$$condition", mergeOperands[1].AsString);
        Assert.Equal(
            AttractionAccessCondition.CurrentProvenanceSchemaVersion,
            mergeOperands[2].AsBsonDocument["provenanceSchemaVersion"]);
    }
}
