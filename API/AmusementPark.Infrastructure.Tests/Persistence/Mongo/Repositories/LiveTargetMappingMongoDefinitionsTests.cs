using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class LiveTargetMappingMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldSupportLatestRevisionLookups()
    {
        IReadOnlyCollection<CreateIndexModel<ExternalLiveTargetMappingDocument>> indexes =
            LiveTargetMappingMongoDefinitions.BuildIndexes();

        CreateIndexModel<ExternalLiveTargetMappingDocument> latestRevision = Assert.Single(
            indexes,
            static index => index.Options.Name == "idx_live_target_mapping_latest_revision");
        BsonDocument keys = latestRevision.Keys.Render(
            new RenderArgs<ExternalLiveTargetMappingDocument>(
                BsonSerializer.LookupSerializer<ExternalLiveTargetMappingDocument>(),
                BsonSerializer.SerializerRegistry));

        Assert.Equal(1, keys["mappingId"].AsInt32);
        Assert.Equal(-1, keys["revision"].AsInt32);
    }

    [Fact]
    public void BuildEligiblePublicTargetCoverageByParkPipeline_ShouldScopeTheConfiguredPilot()
    {
        IReadOnlyCollection<BsonDocument> stages =
            LiveTargetMappingRepository.BuildEligiblePublicTargetCoverageByParkPipeline(
                LiveDataSourceId.Parse("themeparks-wiki"),
                "external-park-1",
                "internal-park-1");

        BsonDocument match = Assert.Single(
            stages,
            static stage => stage.Contains("$match"))["$match"].AsBsonDocument;
        Assert.Equal("themeparks-wiki", match["sourceId"].AsString);
        Assert.Equal("internal-park-1", match["target.parkId"].AsString);
        BsonArray externalScope = match["$or"].AsBsonArray;
        Assert.Contains(
            externalScope,
            static clause => clause.AsBsonDocument.TryGetValue(
                "externalTarget.id",
                out BsonValue? value)
                && value == "external-park-1");
        Assert.Contains(
            externalScope,
            static clause => clause.AsBsonDocument.TryGetValue(
                "externalTarget.parentId",
                out BsonValue? value)
                && value == "external-park-1");
    }

    [Fact]
    public void BuildLatestByExternalEntityPipeline_ShouldFilterAfterLatestRevisionSelection()
    {
        IReadOnlyCollection<BsonDocument> stages =
            LiveTargetMappingRepository.BuildLatestByExternalEntityPipeline(
                LiveDataSourceId.Parse("themeparks-wiki"),
                "external-park-1");

        BsonDocument[] matches = stages
            .Where(static stage => stage.Contains("$match"))
            .Select(static stage => stage["$match"].AsBsonDocument)
            .ToArray();
        Assert.Equal("themeparks-wiki", matches[0]["sourceId"].AsString);
        BsonArray externalScope = matches[1]["$or"].AsBsonArray;
        Assert.Contains(
            externalScope,
            static clause => clause.AsBsonDocument.TryGetValue(
                "externalTarget.id",
                out BsonValue? value)
                && value == "external-park-1");
        Assert.Contains(
            externalScope,
            static clause => clause.AsBsonDocument.TryGetValue(
                "externalTarget.parentId",
                out BsonValue? value)
                && value == "external-park-1");
        int groupIndex = Array.FindIndex(
            stages.ToArray(),
            static stage => stage.Contains("$group"));
        int externalMatchIndex = Array.FindIndex(
            stages.ToArray(),
            static stage => stage.Contains("$match")
                && stage["$match"].AsBsonDocument.Contains("$or"));
        Assert.True(groupIndex < externalMatchIndex);
    }
}
