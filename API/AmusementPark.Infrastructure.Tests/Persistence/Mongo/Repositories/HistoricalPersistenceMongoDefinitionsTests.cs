using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
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
}
