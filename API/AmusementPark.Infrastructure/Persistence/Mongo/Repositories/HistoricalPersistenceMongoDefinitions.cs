using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class HistoricalPersistenceMongoDefinitions
{
    internal static IReadOnlyCollection<CreateIndexModel<HistoricalFactDocument>> BuildFactIndexes()
    {
        return new CreateIndexModel<HistoricalFactDocument>[]
        {
            new CreateIndexModel<HistoricalFactDocument>(
                Builders<HistoricalFactDocument>.IndexKeys
                    .Ascending(document => document.FactId)
                    .Ascending(document => document.Revision),
                new CreateIndexOptions
                {
                    Name = "idx_historical_facts_revision_unique",
                    Unique = true,
                }),
            new CreateIndexModel<HistoricalFactDocument>(
                Builders<HistoricalFactDocument>.IndexKeys
                    .Ascending("subject.type")
                    .Ascending("subject.id")
                    .Ascending("period.start.year"),
                new CreateIndexOptions { Name = "idx_historical_facts_subject_start_year" }),
            new CreateIndexModel<HistoricalFactDocument>(
                Builders<HistoricalFactDocument>.IndexKeys
                    .Ascending("subject.type")
                    .Ascending("subject.id")
                    .Ascending(document => document.Type)
                    .Descending(document => document.Revision),
                new CreateIndexOptions { Name = "idx_historical_facts_subject_type_revision" }),
            new CreateIndexModel<HistoricalFactDocument>(
                Builders<HistoricalFactDocument>.IndexKeys
                    .Ascending(document => document.State)
                    .Descending(document => document.VerifiedAtUtc),
                new CreateIndexOptions { Name = "idx_historical_facts_state_verified" }),
            new CreateIndexModel<HistoricalFactDocument>(
                Builders<HistoricalFactDocument>.IndexKeys
                    .Ascending(document => document.PublicationState)
                    .Ascending(document => document.WorkflowState),
                new CreateIndexOptions { Name = "idx_historical_facts_publication_workflow" }),
            new CreateIndexModel<HistoricalFactDocument>(
                Builders<HistoricalFactDocument>.IndexKeys
                    .Ascending("sources.sourceId")
                    .Ascending("sources.revision"),
                new CreateIndexOptions { Name = "idx_historical_facts_source_revision" }),
        };
    }

    internal static IReadOnlyCollection<CreateIndexModel<HistoricalSourceDocument>> BuildSourceIndexes()
    {
        return new CreateIndexModel<HistoricalSourceDocument>[]
        {
            new CreateIndexModel<HistoricalSourceDocument>(
                Builders<HistoricalSourceDocument>.IndexKeys
                    .Ascending(document => document.SourceId)
                    .Ascending(document => document.Revision),
                new CreateIndexOptions
                {
                    Name = "idx_historical_sources_revision_unique",
                    Unique = true,
                }),
            new CreateIndexModel<HistoricalSourceDocument>(
                Builders<HistoricalSourceDocument>.IndexKeys
                    .Ascending(document => document.PublicationState)
                    .Ascending(document => document.Accessibility),
                new CreateIndexOptions { Name = "idx_historical_sources_publication_access" }),
            new CreateIndexModel<HistoricalSourceDocument>(
                Builders<HistoricalSourceDocument>.IndexKeys
                    .Ascending(document => document.Url),
                new CreateIndexOptions { Name = "idx_historical_sources_url" }),
        };
    }

    internal static IReadOnlyCollection<CreateIndexModel<HistoricalReviewEventDocument>> BuildReviewEventIndexes()
    {
        return new CreateIndexModel<HistoricalReviewEventDocument>[]
        {
            new CreateIndexModel<HistoricalReviewEventDocument>(
                Builders<HistoricalReviewEventDocument>.IndexKeys
                    .Ascending(document => document.ResourceType)
                    .Ascending(document => document.ResourceId)
                    .Descending(document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_historical_reviews_resource_date" }),
            new CreateIndexModel<HistoricalReviewEventDocument>(
                Builders<HistoricalReviewEventDocument>.IndexKeys
                    .Ascending(document => document.ResourceType)
                    .Ascending(document => document.ResourceId)
                    .Ascending(document => document.ResourceRevision)
                    .Ascending(document => document.EventType),
                new CreateIndexOptions { Name = "idx_historical_reviews_revision_event" }),
        };
    }
}
