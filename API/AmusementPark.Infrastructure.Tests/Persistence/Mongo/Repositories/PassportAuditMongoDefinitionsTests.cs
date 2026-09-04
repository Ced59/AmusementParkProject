using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PassportAuditMongoDefinitionsTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 4, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildJournalIndexes_ShouldSupportPrivateTimelineAndEntityRevisionQueries()
    {
        IReadOnlyCollection<CreateIndexModel<PassportAuditJournalDocument>> indexes =
            PassportAuditMongoDefinitions.BuildJournalIndexes();

        Assert.Contains(indexes, index =>
            index.Options.Name == "idx_passport_audit_user_occurred"
            && Render(index.Keys).Equals(new BsonDocument
            {
                { "event.userId", 1 },
                { "event.occurredAtUtc", -1 },
                { "_id", 1 },
            }));
        Assert.Contains(indexes, index =>
            index.Options.Name == "idx_passport_audit_user_visit"
            && Render(index.Keys).Equals(new BsonDocument
            {
                { "event.userId", 1 },
                { "event.visitId", 1 },
                { "_id", 1 },
            }));
        Assert.Contains(indexes, index =>
            index.Options.Name == "idx_passport_audit_entity_revision"
            && Render(index.Keys).Equals(new BsonDocument
            {
                { "event.entityType", 1 },
                { "event.entityId", 1 },
                { "event.entityVersion", 1 },
            }));
    }

    [Fact]
    public void PendingMarkerIndex_ShouldBePartialAndBoundedToMarkedSources()
    {
        CreateIndexModel<UserVisitDocument> index =
            PassportAuditMongoDefinitions.BuildPendingMarkerIndex<UserVisitDocument>(
                "idx_pending");

        Assert.Equal(
            new BsonDocument("pendingAuditEvents.eventId", 1),
            Render(index.Keys));
        Assert.NotNull(index.Options.PartialFilterExpression);
        Assert.Equal(
            new BsonDocument(
                "pendingAuditEvents.eventId",
                new BsonDocument("$exists", true)),
            Render(index.Options.PartialFilterExpression!));
    }

    [Fact]
    public void Mapper_ShouldRoundTripOnlyTheMinimizedEvidence()
    {
        PassportAuditEvent auditEvent = PassportAuditEvent.Create(
            "user-1",
            PassportAuditEntityType.RideAssessment,
            "ride-1",
            "visit-1",
            "park-1",
            "item-1",
            PassportAuditEventType.RideAssessmentChanged,
            4,
            2,
            new[]
            {
                PassportAuditChangedField.RideAssessmentRating,
                PassportAuditChangedField.RideAssessmentPrivateComment,
            },
            8,
            9,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            true,
            "operation-1",
            PassportAuditOrigin.User,
            NowUtc);

        PassportAuditEventDocument document = auditEvent.ToDocument();
        PassportAuditEvent restored = document.ToDomain();

        Assert.Equal(auditEvent.Id, restored.Id);
        Assert.Equal((byte)8, restored.PreviousRatingHalfSteps);
        Assert.Equal((byte)9, restored.NewRatingHalfSteps);
        Assert.True(restored.PrivateTextChanged);
        Assert.Equal(auditEvent.ChangedFields, restored.ChangedFields);
        BsonDocument serialized = document.ToBsonDocument();
        Assert.False(serialized.Contains("privateComment"));
        Assert.False(serialized.Contains("privateNote"));
    }

    [Fact]
    public void MongoSettings_ShouldUseADedicatedAuditCollectionByDefault()
    {
        MongoDbSettings settings = new MongoDbSettings();

        Assert.Equal("passport-audit-events", settings.PassportAuditEventsCollectionName);
    }

    [Fact]
    public void Mapper_ShouldRoundTripVisitDatesWithoutInventingPrecision()
    {
        PassportAuditEvent auditEvent = PassportAuditEvent.Create(
            "user-1",
            PassportAuditEntityType.Visit,
            "visit-1",
            "visit-1",
            "park-1",
            null,
            PassportAuditEventType.VisitDateChanged,
            2,
            null,
            new[] { PassportAuditChangedField.Date },
            null,
            null,
            VisitDate.ForMonth(2024, 7, true),
            VisitDate.ForYear(2023),
            VisitStatus.Draft,
            VisitStatus.Draft,
            null,
            null,
            null,
            null,
            false,
            "operation-2",
            PassportAuditOrigin.User,
            NowUtc);

        PassportAuditEvent restored = auditEvent.ToDocument().ToDomain();

        Assert.Equal(VisitDate.ForMonth(2024, 7, true), restored.PreviousVisitDate);
        Assert.Equal(VisitDate.ForYear(2023), restored.NewVisitDate);
    }

    private static BsonDocument Render<TDocument>(IndexKeysDefinition<TDocument> keys)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        RenderArgs<TDocument> arguments = new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);
        return keys.Render(arguments);
    }

    private static BsonDocument Render<TDocument>(FilterDefinition<TDocument> filter)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        RenderArgs<TDocument> arguments = new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
