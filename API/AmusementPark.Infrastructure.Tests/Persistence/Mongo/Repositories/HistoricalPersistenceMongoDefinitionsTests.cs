using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class HistoricalPersistenceMongoDefinitionsTests
{
    [Fact]
    public void BuildFactIndexes_ShouldProtectImmutableRevisionsAndQueryPaths()
    {
        IReadOnlyCollection<CreateIndexModel<HistoricalFactDocument>> indexes =
            HistoricalPersistenceMongoDefinitions.BuildFactIndexes();

        CreateIndexModel<HistoricalFactDocument> revision = indexes.Single(
            index => index.Options.Name == "idx_historical_facts_revision_unique");
        Assert.True(revision.Options.Unique);
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_facts_subject_start_year");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_facts_publication_workflow");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_facts_source_revision");
        Assert.All(indexes, index => Assert.Null(index.Options.ExpireAfter));
    }

    [Fact]
    public void BuildSourceIndexes_ShouldProtectImmutableRevisionsWithoutExpiration()
    {
        IReadOnlyCollection<CreateIndexModel<HistoricalSourceDocument>> indexes =
            HistoricalPersistenceMongoDefinitions.BuildSourceIndexes();

        CreateIndexModel<HistoricalSourceDocument> revision = indexes.Single(
            index => index.Options.Name == "idx_historical_sources_revision_unique");
        Assert.True(revision.Options.Unique);
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_sources_publication_access");
        Assert.All(indexes, index => Assert.Null(index.Options.ExpireAfter));
    }

    [Fact]
    public void BuildReviewEventIndexes_ShouldKeepPermanentResourceAuditPaths()
    {
        IReadOnlyCollection<CreateIndexModel<HistoricalReviewEventDocument>> indexes =
            HistoricalPersistenceMongoDefinitions.BuildReviewEventIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_reviews_resource_date");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_reviews_revision_event");
        Assert.All(indexes, index => Assert.Null(index.Options.ExpireAfter));
    }

    [Fact]
    public void BuildLatestRevisionsPipeline_ShouldSelectOneRevisionPerSourceOnServer()
    {
        IReadOnlyCollection<BsonDocument> stages =
            HistoricalSourceRepository.BuildLatestRevisionsPipeline(new[] { "source-1", "source-2" });
        BsonDocument[] pipeline = stages.ToArray();

        Assert.Equal(5, pipeline.Length);
        Assert.True(pipeline[0].Contains("$match"));
        Assert.Equal(-1, pipeline[1]["$sort"]["revision"].AsInt32);
        Assert.Equal("$$ROOT", pipeline[2]["$group"]["document"]["$first"].AsString);
        Assert.True(pipeline[3].Contains("$replaceRoot"));
    }

    [Fact]
    public void IsStandaloneAttractionPublic_WhenAttractionIsClosedDefinitively_ShouldReturnFalse()
    {
        StandaloneAttractionDocument attraction = new StandaloneAttractionDocument
        {
            IsVisible = true,
            AttractionDetails = new AttractionDetailsDocument
            {
                Status = "permanently closed",
            },
        };

        bool isPublic = HistoricalSubjectPublicationStateReader.IsStandaloneAttractionPublic(attraction);

        Assert.False(isPublic);
    }
}
