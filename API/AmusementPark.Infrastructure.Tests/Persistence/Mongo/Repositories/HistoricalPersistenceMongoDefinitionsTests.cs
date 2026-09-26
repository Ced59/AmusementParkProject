using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_facts_park_scope_revision");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_facts_publication_workflow");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_facts_source_revision");
        CreateIndexModel<HistoricalFactDocument> audit = indexes.Single(
            index => index.Options.Name == "idx_historical_facts_audit_date");
        IBsonSerializer<HistoricalFactDocument> factSerializer =
            BsonSerializer.SerializerRegistry.GetSerializer<HistoricalFactDocument>();
        BsonDocument auditKeys = audit.Keys.Render(
            new RenderArgs<HistoricalFactDocument>(
                factSerializer,
                BsonSerializer.SerializerRegistry));
        Assert.Equal(-1, auditKeys["transitionReviewEvent.occurredAtUtc"].AsInt32);
        Assert.Equal(-1, auditKeys["revision"].AsInt32);
        Assert.All(indexes, index => Assert.Null(index.Options.ExpireAfter));
    }

    [Fact]
    public void BuildLatestDecisionEligibleForParkPipeline_ShouldDiscoverScopedRetiredSubjectsAfterGroupingLatestRevisions()
    {
        HistoricalSubject currentPark = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);

        BsonDocument[] pipeline = HistoricalFactRepository
            .BuildLatestDecisionEligibleForParkPipeline(
                "park-1",
                new[] { currentPark },
                new[] { "fact-1", "fact-2" })
            .ToArray();

        Assert.Equal(5, pipeline.Length);
        BsonArray candidateIds = pipeline[0]["$match"]["factId"]["$in"].AsBsonArray;
        Assert.Equal(new[] { "fact-1", "fact-2" }, candidateIds.Select(static id => id.AsString));
        Assert.Equal("$$ROOT", pipeline[2]["$group"]["document"]["$first"].AsString);
        BsonArray publicEligibility = pipeline[4]["$match"]["$or"].AsBsonArray;
        Assert.Contains(
            publicEligibility,
            filter => filter.AsBsonDocument.GetValue("subject.publicationPolicy", BsonNull.Value)
                == HistoricalSubjectPublicationPolicy.HistoricalOnly.ToString());
    }

    [Fact]
    public void BuildParkCandidateScopeFilter_ShouldFindCurrentAndDurablyScopedFactChains()
    {
        HistoricalSubject currentPark = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);

        BsonDocument filter = HistoricalFactRepository.BuildParkCandidateScopeFilter(
            "park-1",
            new[] { currentPark });

        BsonArray alternatives = filter["$or"].AsBsonArray;
        Assert.Contains(
            alternatives,
            alternative => alternative.AsBsonDocument.GetValue("subject.id", BsonNull.Value)
                == "park-1");
        Assert.Contains(
            alternatives,
            alternative => alternative.AsBsonDocument.GetValue("subject.contextParkId", BsonNull.Value)
                == "park-1");
    }

    [Fact]
    public void BuildSourceIndexes_ShouldProtectImmutableRevisionsWithoutExpiration()
    {
        IReadOnlyCollection<CreateIndexModel<HistoricalSourceDocument>> indexes =
            HistoricalPersistenceMongoDefinitions.BuildSourceIndexes();

        CreateIndexModel<HistoricalSourceDocument> revision = indexes.Single(
            index => index.Options.Name == "idx_historical_sources_revision_unique");
        Assert.True(revision.Options.Unique);
        CreateIndexModel<HistoricalSourceDocument> latestRevision = indexes.Single(
            index => index.Options.Name == "idx_historical_sources_latest_revision");
        IBsonSerializer<HistoricalSourceDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<HistoricalSourceDocument>();
        BsonDocument latestRevisionKeys = latestRevision.Keys.Render(
            new RenderArgs<HistoricalSourceDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));
        Assert.Equal(1, latestRevisionKeys["sourceId"].AsInt32);
        Assert.Equal(-1, latestRevisionKeys["revision"].AsInt32);
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_sources_publication_access");
        CreateIndexModel<HistoricalSourceDocument> audit = indexes.Single(
            index => index.Options.Name == "idx_historical_sources_audit_date");
        BsonDocument auditKeys = audit.Keys.Render(
            new RenderArgs<HistoricalSourceDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));
        Assert.Equal(-1, auditKeys["transitionReviewEvent.occurredAtUtc"].AsInt32);
        Assert.Equal(-1, auditKeys["revision"].AsInt32);
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

    [Fact]
    public void BuildFactAuditFilter_WhenCursorExists_ShouldSelectOnlyOlderRevisions()
    {
        DateTime occurredAtUtc = new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);
        HistoricalAuditCursor cursor = new HistoricalAuditCursor(occurredAtUtc, 42);

        FilterDefinition<HistoricalFactDocument> filter =
            HistoricalReviewEventRepository.BuildFactFilter("fact-1", cursor);
        IBsonSerializer<HistoricalFactDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<HistoricalFactDocument>();
        BsonDocument rendered = filter.Render(
            new RenderArgs<HistoricalFactDocument>(serializer, BsonSerializer.SerializerRegistry));

        Assert.Equal("fact-1", rendered["factId"].AsString);
        BsonArray alternatives = rendered["$or"].AsBsonArray;
        Assert.Equal(2, alternatives.Count);
        Assert.True(alternatives[0]["transitionReviewEvent.occurredAtUtc"].AsBsonDocument.Contains("$lt"));
        Assert.Equal(42, alternatives[1]["revision"]["$lt"].AsInt32);
    }

    [Fact]
    public void BuildSourceAuditFilter_WhenCursorExists_ShouldSelectOnlyOlderRevisions()
    {
        DateTime occurredAtUtc = new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);
        HistoricalAuditCursor cursor = new HistoricalAuditCursor(occurredAtUtc, 17);

        FilterDefinition<HistoricalSourceDocument> filter =
            HistoricalReviewEventRepository.BuildSourceFilter("source-1", cursor);
        IBsonSerializer<HistoricalSourceDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<HistoricalSourceDocument>();
        BsonDocument rendered = filter.Render(
            new RenderArgs<HistoricalSourceDocument>(serializer, BsonSerializer.SerializerRegistry));

        Assert.Equal("source-1", rendered["sourceId"].AsString);
        BsonArray alternatives = rendered["$or"].AsBsonArray;
        Assert.Equal(2, alternatives.Count);
        Assert.True(alternatives[0]["transitionReviewEvent.occurredAtUtc"].AsBsonDocument.Contains("$lt"));
        Assert.Equal(17, alternatives[1]["revision"]["$lt"].AsInt32);
    }

    [Fact]
    public void BuildAuditPage_WhenAnotherBatchExists_ShouldReturnCursorFromLastVisibleEvent()
    {
        DateTime occurredAtUtc = new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);
        HistoricalReviewEventDocument[] documents =
        {
            CreateReviewEventDocument(3, occurredAtUtc.AddMinutes(2)),
            CreateReviewEventDocument(2, occurredAtUtc.AddMinutes(1)),
            CreateReviewEventDocument(1, occurredAtUtc),
        };

        HistoricalAuditPage page = HistoricalReviewEventRepository.BuildPage(documents, 2);

        Assert.Equal(2, page.Items.Count);
        Assert.NotNull(page.NextCursor);
        Assert.Equal(2, page.NextCursor.ResourceRevision);
        Assert.Equal(occurredAtUtc.AddMinutes(1), page.NextCursor.OccurredAtUtc);
    }

    private static HistoricalReviewEventDocument CreateReviewEventDocument(
        int revision,
        DateTime occurredAtUtc)
    {
        return new HistoricalReviewEventDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            ResourceType = HistoricalReviewResourceType.Fact,
            ResourceId = Guid.NewGuid().ToString("N"),
            ResourceRevision = revision,
            EventType = HistoricalReviewEventType.ReviewUpdated,
            ActorUserId = "reviewer-1",
            OccurredAtUtc = occurredAtUtc,
        };
    }
}
