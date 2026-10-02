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
    public void BuildRelationIndexes_ShouldProtectImmutableRevisionsAndReadPaths()
    {
        IReadOnlyCollection<CreateIndexModel<HistoricalRelationDocument>> indexes =
            HistoricalPersistenceMongoDefinitions.BuildRelationIndexes();

        CreateIndexModel<HistoricalRelationDocument> revision = indexes.Single(
            index => index.Options.Name == "idx_historical_relations_revision_unique");
        Assert.True(revision.Options.Unique);
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_relations_source_type_revision");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_relations_target_type_revision");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_relations_source_park_revision");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_relations_target_park_revision");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_relations_publication_state");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_relations_source_revision");
        Assert.Contains(indexes, index => index.Options.Name == "idx_historical_relations_audit_date");
        CreateIndexModel<HistoricalRelationDocument> source = indexes.Single(
            index => index.Options.Name == "idx_historical_relations_source_type_revision");
        IBsonSerializer<HistoricalRelationDocument> relationSerializer =
            BsonSerializer.SerializerRegistry.GetSerializer<HistoricalRelationDocument>();
        BsonDocument sourceKeys = source.Keys.Render(
            new RenderArgs<HistoricalRelationDocument>(
                relationSerializer,
                BsonSerializer.SerializerRegistry));
        Assert.True(sourceKeys.Contains("source.contextParkId"));
        Assert.All(indexes, index => Assert.Null(index.Options.ExpireAfter));
    }

    [Fact]
    public void BuildLatestForParkPipelines_ShouldReloadLatestThenReapplyScopeWithoutPublicFiltering()
    {
        HistoricalSubject park = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);

        BsonDocument[] factPipeline = HistoricalFactRepository.BuildLatestForParkPipeline(
                "park-1",
                new[] { park },
                "historical-facts")
            .ToArray();
        BsonDocument[] relationPipeline = HistoricalRelationRepository.BuildLatestForParkPipeline(
                "park-1",
                new[] { new HistoricalSubjectKey(HistoricalSubjectType.Park, "park-1") },
                "historical-relations")
            .ToArray();

        Assert.Equal("$factId", factPipeline[1]["$group"]["_id"].AsString);
        Assert.Equal(-1, factPipeline[2]["$lookup"]["pipeline"][1]["$sort"]["revision"].AsInt32);
        Assert.True(factPipeline[^1]["$match"].AsBsonDocument.Contains("$or"));
        Assert.DoesNotContain(factPipeline, stage =>
            stage.Contains("$match")
            && stage["$match"].AsBsonDocument.Contains("publicationState"));
        Assert.Equal("$relationId", relationPipeline[1]["$group"]["_id"].AsString);
        Assert.Equal(-1, relationPipeline[2]["$lookup"]["pipeline"][1]["$sort"]["revision"].AsInt32);
        Assert.True(relationPipeline[^2]["$match"].AsBsonDocument.Contains("$or"));
        Assert.DoesNotContain(relationPipeline, stage =>
            stage.Contains("$match")
            && stage["$match"].AsBsonDocument.Contains("publicationState"));
    }

    [Fact]
    public void BuildLatestTouchingSubjectsPipeline_ShouldReloadLatestRevisionBeforePublicFilter()
    {
        IReadOnlyCollection<BsonDocument> stages =
            HistoricalRelationRepository.BuildLatestTouchingSubjectsPipeline(
                new[] { new HistoricalSubjectKey(HistoricalSubjectType.ParkItem, "item-1", "park-1") },
                "historical-relations",
                25);
        BsonDocument[] pipeline = stages.ToArray();

        Assert.Equal("$relationId", pipeline[1]["$group"]["_id"].AsString);
        Assert.Equal("historical-relations", pipeline[2]["$lookup"]["from"].AsString);
        Assert.Equal(-1, pipeline[2]["$lookup"]["pipeline"][1]["$sort"]["revision"].AsInt32);
        Assert.Equal("$latest", pipeline[4]["$replaceRoot"]["newRoot"].AsString);
        Assert.True(pipeline[5]["$match"].AsBsonDocument.Contains("$or"));
        Assert.Contains(
            pipeline[5]["$match"]["$or"].AsBsonArray,
            endpoint => endpoint.AsBsonDocument.Contains("source.contextParkId")
                && endpoint.AsBsonDocument["source.contextParkId"] == "park-1");
        Assert.Equal(HistoricalPublicationState.Published.ToString(), pipeline[6]["$match"]["publicationState"].AsString);
        Assert.Equal(25, pipeline[^1]["$limit"].AsInt32);
    }

    [Fact]
    public void BuildLatestTouchingEachSubjectPipeline_ShouldGiveEverySubjectItsOwnBoundedFacet()
    {
        IReadOnlyCollection<BsonDocument> stages =
            HistoricalRelationRepository.BuildLatestTouchingEachSubjectPipeline(
                new[]
                {
                    new HistoricalSubjectKey(HistoricalSubjectType.ParkItem, "dense-item", "park-1"),
                    new HistoricalSubjectKey(HistoricalSubjectType.ParkItem, "later-item", "park-1"),
                },
                "historical-relations",
                200);
        BsonDocument[] pipeline = stages.ToArray();
        BsonDocument facets = pipeline[0]["$facet"].AsBsonDocument;

        Assert.Equal(2, facets.ElementCount);
        Assert.All(
            facets.Elements,
            facet => Assert.Equal(200, facet.Value.AsBsonArray[^1]["$limit"].AsInt32));
        Assert.Equal("$relationId", pipeline[4]["$group"]["_id"].AsString);
        Assert.Equal("$relation", pipeline[5]["$replaceRoot"]["newRoot"].AsString);
    }

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
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");

        BsonDocument[] pipeline = HistoricalFactRepository
            .BuildLatestDecisionEligibleForParkPipeline(
                "park-1",
                new[] { currentPark },
                "historical-facts")
            .ToArray();

        Assert.Equal(7, pipeline.Length);
        Assert.True(pipeline[0]["$match"].AsBsonDocument.Contains("$or"));
        Assert.Equal("$factId", pipeline[1]["$group"]["_id"].AsString);
        BsonDocument lookup = pipeline[2]["$lookup"].AsBsonDocument;
        Assert.Equal("historical-facts", lookup["from"].AsString);
        BsonArray lookupPipeline = lookup["pipeline"].AsBsonArray;
        Assert.Equal(3, lookupPipeline.Count);
        Assert.Equal(-1, lookupPipeline[1]["$sort"]["revision"].AsInt32);
        Assert.Equal(1, lookupPipeline[2]["$limit"].AsInt32);
        Assert.Equal("$latestRevision", pipeline[4]["$replaceRoot"]["newRoot"].AsString);
        Assert.True(pipeline[5]["$match"].AsBsonDocument.Contains("$or"));
        BsonArray publicEligibility = pipeline[6]["$match"]["$or"].AsBsonArray;
        Assert.Contains(
            publicEligibility,
            filter => filter.AsBsonDocument.GetValue("subject.publicationPolicy", BsonNull.Value)
                == HistoricalSubjectPublicationPolicy.HistoricalOnly.ToString());
        Assert.Contains(
            publicEligibility,
            filter => filter.AsBsonDocument.GetValue("subject.id", BsonNull.Value) == "park-1"
                && filter.AsBsonDocument.GetValue("subject.publicationPolicy", BsonNull.Value)
                    == HistoricalSubjectPublicationPolicy.FollowCurrentSubject.ToString());
    }

    [Fact]
    public void BuildLatestPublicTimelineForParkPipeline_ShouldKeepReviewedAndLegacyPublishedFactsDistinct()
    {
        HistoricalSubject currentPark = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");

        BsonDocument[] pipeline = HistoricalFactRepository
            .BuildLatestPublicTimelineForParkPipeline(
                "park-1",
                new[] { currentPark },
                "historical-facts")
            .ToArray();

        BsonArray conditions = pipeline[6]["$match"]["$and"].AsBsonArray;
        BsonArray lifecycleAlternatives = conditions[0]["$or"].AsBsonArray;
        Assert.Contains(
            lifecycleAlternatives,
            alternative => alternative["publicationState"]
                == HistoricalPublicationState.Published.ToString());
        Assert.Contains(
            lifecycleAlternatives,
            alternative => alternative["publicationState"]
                    == HistoricalPublicationState.LegacyPublishedPendingReview.ToString()
                && alternative["state"] == HistoricalFactState.Unverified.ToString()
                && alternative["revisionOrigin"] == HistoricalRevisionOrigin.LegacyMigration.ToString());
        BsonArray publicSubjects = conditions[1]["$or"].AsBsonArray;
        Assert.Contains(
            publicSubjects,
            filter => filter.AsBsonDocument.GetValue("subject.id", BsonNull.Value) == "park-1");
    }

    [Fact]
    public void BuildLatestLegacyPublicTimelineForParkPipeline_ShouldExcludeOrdinaryPublishedFacts()
    {
        HistoricalSubject currentPark = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");

        BsonDocument[] pipeline = HistoricalFactRepository
            .BuildLatestLegacyPublicTimelineForParkPipeline(
                "park-1",
                new[] { currentPark },
                "historical-facts")
            .ToArray();

        BsonDocument lifecycleFilter = pipeline[6]["$match"].AsBsonDocument;
        Assert.Equal(
            HistoricalPublicationState.LegacyPublishedPendingReview.ToString(),
            lifecycleFilter["publicationState"].AsString);
        Assert.Equal(HistoricalFactState.Unverified.ToString(), lifecycleFilter["state"].AsString);
        Assert.Equal(
            HistoricalRevisionOrigin.LegacyMigration.ToString(),
            lifecycleFilter["revisionOrigin"].AsString);
        Assert.True(lifecycleFilter.Contains("$or"));
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
                == "park-1"
                && alternative.AsBsonDocument.GetValue("subject.contextParkId", BsonNull.Value)
                    == "park-1"
                && alternative.AsBsonDocument.GetValue(
                    "subject.publicationPolicy",
                    BsonNull.Value)
                    == HistoricalSubjectPublicationPolicy.FollowCurrentSubject.ToString());
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
    public void BuildPublicKeysPreservingContext_WhenOperatorIsPublic_ShouldKeepEveryCandidateContext()
    {
        HistoricalSubject[] candidates =
        {
            new HistoricalSubject(
                HistoricalSubjectType.ParkOperator,
                "operator-1",
                "Exploitant",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                "park-1"),
            new HistoricalSubject(
                HistoricalSubjectType.ParkOperator,
                "operator-1",
                "Exploitant",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                "park-2"),
        };

        HistoricalSubjectKey[] keys = HistoricalSubjectPublicationStateReader
            .BuildPublicKeysPreservingContext(
                HistoricalSubjectType.ParkOperator,
                candidates,
                new[] { "operator-1" });

        Assert.Contains(keys, static key => key.ContextParkId == "park-1");
        Assert.Contains(keys, static key => key.ContextParkId == "park-2");
        Assert.All(keys, static key => Assert.Equal("operator-1", key.Id));
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
