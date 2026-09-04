using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PassportAuditStoreTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 4, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ContentFenceAllowsAuditDelivery_ShouldExposeOnlyTheSafeGenerationInterval()
    {
        Assert.True(PassportAuditStore.ContentFenceAllowsAuditDelivery(
            null,
            false,
            null,
            null));
        Assert.False(PassportAuditStore.ContentFenceAllowsAuditDelivery(
            null,
            false,
            null,
            1));
        Assert.True(PassportAuditStore.ContentFenceAllowsAuditDelivery(7, true, 7, 7));
        Assert.False(PassportAuditStore.ContentFenceAllowsAuditDelivery(7, true, 7, 6));
        Assert.True(PassportAuditStore.ContentFenceAllowsAuditDelivery(7, false, 6, 6));
        Assert.True(PassportAuditStore.ContentFenceAllowsAuditDelivery(7, false, 6, 7));
        Assert.False(PassportAuditStore.ContentFenceAllowsAuditDelivery(7, false, 6, 5));
        Assert.False(PassportAuditStore.ContentFenceAllowsAuditDelivery(7, false, 6, null));
        Assert.True(PassportAuditStore.ContentFenceAllowsAuditDelivery(7, false, null, null));
        Assert.True(PassportAuditStore.ContentFenceAllowsAuditDelivery(7, false, null, 1));
        Assert.False(PassportAuditStore.ContentFenceAllowsAuditDelivery(7, false, null, 8));
    }

    [Fact]
    public async Task TryPublishAsync_ShouldAppendBeforeAcknowledgingTheSourceMarker()
    {
        PassportAuditEvent auditEvent = CreateVisitAuditEvent();
        UserVisitDocument source = new UserVisitDocument
        {
            Id = "visit-1",
            UserId = "user-1",
            PendingAuditEvents = new List<PassportAuditEventDocument>
            {
                auditEvent.ToDocument(),
            },
        };
        Mock<IMongoCollection<PassportAuditJournalDocument>> auditCollection =
            new Mock<IMongoCollection<PassportAuditJournalDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserVisitDocument>> visitCollection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceDocument>> occurrenceCollection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        Mock<IAsyncCursor<BsonDocument>> cursor = CreateAsyncCursor(new[]
        {
            source.ToBsonDocument(),
        });
        Mock<IAsyncCursor<BsonDocument>> emptyOccurrenceCursor =
            CreateAsyncCursor(Array.Empty<BsonDocument>());
        Mock<IAsyncCursor<BsonDocument>> emptyOperationCursor =
            CreateAsyncCursor(Array.Empty<BsonDocument>());
        PassportAuditJournalDocument? appended = null;
        visitCollection.Setup(value => value.FindAsync<BsonDocument>(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<FindOptions<UserVisitDocument, BsonDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(cursor.Object);
        occurrenceCollection.Setup(value => value.FindAsync<BsonDocument>(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument, BsonDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(emptyOccurrenceCursor.Object);
        operationCollection.Setup(value => value.FindAsync<BsonDocument>(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    BsonDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(emptyOperationCursor.Object);
        auditCollection.Setup(value => value.InsertOneAsync(
                It.IsAny<PassportAuditJournalDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback((
                PassportAuditJournalDocument document,
                InsertOneOptions _,
                CancellationToken _) => appended = document)
            .Returns(Task.CompletedTask);
        SetupAcknowledgement(visitCollection);
        SetupAcknowledgement(occurrenceCollection);
        SetupAcknowledgement(operationCollection);
        SetupMaintenanceLease(visitCollection);
        TestLogger logger = new TestLogger();
        PassportAuditStore store = CreateStore(
            auditCollection.Object,
            visitCollection.Object,
            occurrenceCollection.Object,
            operationCollection.Object,
            logger);

        bool published = await store.TryPublishAsync(auditEvent, CancellationToken.None);

        Assert.True(published, logger.LastException?.ToString());
        Assert.NotNull(appended);
        Assert.Equal(auditEvent.Id, appended.Id);
        Assert.Equal(auditEvent.Id, appended.Event.EventId);
        auditCollection.VerifyAll();
        visitCollection.VerifyAll();
        occurrenceCollection.VerifyAll();
        operationCollection.VerifyAll();
        cursor.VerifyAll();
        emptyOccurrenceCursor.VerifyAll();
        emptyOperationCursor.VerifyAll();
    }

    [Fact]
    public async Task TryPublishBatchAsync_ShouldBatchEventsFromTheSameVisitUnderOneLease()
    {
        IReadOnlyCollection<PassportAuditEvent> auditEvents = CreateVisitAuditBatch();
        UserVisitDocument source = new UserVisitDocument
        {
            Id = "visit-1",
            UserId = "user-1",
            PendingAuditEvents = auditEvents
                .Select(static auditEvent => auditEvent.ToDocument())
                .ToList(),
        };
        Mock<IMongoCollection<PassportAuditJournalDocument>> auditCollection =
            new Mock<IMongoCollection<PassportAuditJournalDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserVisitDocument>> visitCollection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceDocument>> occurrenceCollection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        Mock<IAsyncCursor<BsonDocument>> cursor = CreateAsyncCursor(new[]
        {
            source.ToBsonDocument(),
        });
        Mock<IAsyncCursor<BsonDocument>> emptyOccurrenceCursor =
            CreateAsyncCursor(Array.Empty<BsonDocument>());
        Mock<IAsyncCursor<BsonDocument>> emptyOperationCursor =
            CreateAsyncCursor(Array.Empty<BsonDocument>());
        IReadOnlyCollection<PassportAuditJournalDocument>? appended = null;
        visitCollection.Setup(value => value.FindAsync<BsonDocument>(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<FindOptions<UserVisitDocument, BsonDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(cursor.Object);
        occurrenceCollection.Setup(value => value.FindAsync<BsonDocument>(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument, BsonDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(emptyOccurrenceCursor.Object);
        operationCollection.Setup(value => value.FindAsync<BsonDocument>(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    BsonDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(emptyOperationCursor.Object);
        auditCollection.Setup(value => value.InsertManyAsync(
                It.IsAny<IEnumerable<PassportAuditJournalDocument>>(),
                It.Is<InsertManyOptions>(static options => !options.IsOrdered),
                CancellationToken.None))
            .Callback((
                IEnumerable<PassportAuditJournalDocument> documents,
                InsertManyOptions _,
                CancellationToken _) => appended = documents.ToArray())
            .Returns(Task.CompletedTask);
        SetupAcknowledgement(visitCollection);
        SetupAcknowledgement(occurrenceCollection);
        SetupAcknowledgement(operationCollection);
        SetupMaintenanceLease(visitCollection);
        TestLogger logger = new TestLogger();
        PassportAuditStore store = CreateStore(
            auditCollection.Object,
            visitCollection.Object,
            occurrenceCollection.Object,
            operationCollection.Object,
            logger);

        bool published = await store.TryPublishBatchAsync(
            auditEvents,
            CancellationToken.None);

        Assert.True(published, logger.LastException?.ToString());
        Assert.NotNull(appended);
        Assert.Equal(2, appended.Count);
        Assert.Equal(
            auditEvents.Select(static auditEvent => auditEvent.Id).Order(),
            appended.Select(static document => document.Id).Order());
        visitCollection.Verify(value => value.UpdateOneAsync(
            It.IsAny<FilterDefinition<UserVisitDocument>>(),
            It.IsAny<UpdateDefinition<UserVisitDocument>>(),
            It.IsAny<UpdateOptions>(),
            CancellationToken.None), Times.Exactly(2));
        visitCollection.Verify(value => value.UpdateManyAsync(
            It.IsAny<FilterDefinition<UserVisitDocument>>(),
            It.IsAny<UpdateDefinition<UserVisitDocument>>(),
            It.IsAny<UpdateOptions>(),
            CancellationToken.None), Times.Once);
        occurrenceCollection.Verify(value => value.UpdateManyAsync(
            It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
            It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
            It.IsAny<UpdateOptions>(),
            CancellationToken.None), Times.Once);
        operationCollection.Verify(value => value.UpdateManyAsync(
            It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
            It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
            It.IsAny<UpdateOptions>(),
            CancellationToken.None), Times.Once);
        auditCollection.VerifyAll();
        visitCollection.VerifyAll();
        occurrenceCollection.VerifyAll();
        operationCollection.VerifyAll();
        cursor.VerifyAll();
        emptyOccurrenceCursor.VerifyAll();
        emptyOperationCursor.VerifyAll();
    }

    [Fact]
    public async Task TryPublishAsync_WithoutDurableMarker_ShouldNotAppendAnEvent()
    {
        PassportAuditEvent auditEvent = CreateVisitAuditEvent();
        Mock<IMongoCollection<PassportAuditJournalDocument>> auditCollection =
            new Mock<IMongoCollection<PassportAuditJournalDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserVisitDocument>> visitCollection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceDocument>> occurrenceCollection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        Mock<IAsyncCursor<BsonDocument>> visitCursor =
            CreateAsyncCursor(Array.Empty<BsonDocument>());
        Mock<IAsyncCursor<BsonDocument>> occurrenceCursor =
            CreateAsyncCursor(Array.Empty<BsonDocument>());
        Mock<IAsyncCursor<BsonDocument>> operationCursor =
            CreateAsyncCursor(Array.Empty<BsonDocument>());
        visitCollection.Setup(value => value.FindAsync<BsonDocument>(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<FindOptions<UserVisitDocument, BsonDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(visitCursor.Object);
        occurrenceCollection.Setup(value => value.FindAsync<BsonDocument>(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument, BsonDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(occurrenceCursor.Object);
        operationCollection.Setup(value => value.FindAsync<BsonDocument>(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    BsonDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(operationCursor.Object);
        SetupMaintenanceLease(visitCollection);
        TestLogger logger = new TestLogger();
        PassportAuditStore store = CreateStore(
            auditCollection.Object,
            visitCollection.Object,
            occurrenceCollection.Object,
            operationCollection.Object,
            logger);

        bool published = await store.TryPublishAsync(auditEvent, CancellationToken.None);

        Assert.True(published, logger.LastException?.ToString());
        auditCollection.VerifyNoOtherCalls();
        visitCollection.VerifyAll();
        occurrenceCollection.VerifyAll();
        operationCollection.VerifyAll();
        visitCursor.VerifyAll();
        occurrenceCursor.VerifyAll();
        operationCursor.VerifyAll();
    }

    [Fact]
    public async Task TryPublishAsync_WhenPurgeOwnsTheLease_ShouldNotLoadOrAppendTheEvent()
    {
        PassportAuditEvent auditEvent = CreateVisitAuditEvent();
        Mock<IMongoCollection<PassportAuditJournalDocument>> auditCollection =
            new Mock<IMongoCollection<PassportAuditJournalDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserVisitDocument>> visitCollection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceDocument>> occurrenceCollection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        visitCollection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));
        PassportAuditStore store = CreateStore(
            auditCollection.Object,
            visitCollection.Object,
            occurrenceCollection.Object,
            operationCollection.Object,
            NullLogger<PassportAuditStore>.Instance);

        bool published = await store.TryPublishAsync(auditEvent, CancellationToken.None);

        Assert.False(published);
        visitCollection.VerifyAll();
        auditCollection.VerifyNoOtherCalls();
        occurrenceCollection.VerifyNoOtherCalls();
        operationCollection.VerifyNoOtherCalls();
    }

    private static PassportAuditStore CreateStore(
        IMongoCollection<PassportAuditJournalDocument> auditCollection,
        IMongoCollection<UserVisitDocument> visitCollection,
        IMongoCollection<UserRideOccurrenceDocument> occurrenceCollection,
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> operationCollection,
        ILogger<PassportAuditStore> logger)
    {
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<PassportAuditJournalDocument>(
                "passport-audit-events",
                null))
            .Returns(auditCollection);
        database.Setup(value => value.GetCollection<UserVisitDocument>("user-visits", null))
            .Returns(visitCollection);
        database.Setup(value => value.GetCollection<UserRideOccurrenceDocument>(
                "user-ride-occurrences",
                null))
            .Returns(occurrenceCollection);
        database.Setup(value => value.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                "user-ride-occurrence-operations",
                null))
            .Returns(operationCollection);
        return new PassportAuditStore(
            database.Object,
            new MongoDbSettings(),
            logger);
    }

    private static void SetupAcknowledgement<TDocument>(
        Mock<IMongoCollection<TDocument>> collection)
    {
        collection.Setup(value => value.UpdateManyAsync(
                It.IsAny<FilterDefinition<TDocument>>(),
                It.IsAny<UpdateDefinition<TDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
    }

    private static void SetupMaintenanceLease(
        Mock<IMongoCollection<UserVisitDocument>> collection)
    {
        collection.SetupSequence(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
    }

    private static Mock<IAsyncCursor<TDocument>> CreateAsyncCursor<TDocument>(
        IReadOnlyCollection<TDocument> values)
    {
        Mock<IAsyncCursor<TDocument>> cursor =
            new Mock<IAsyncCursor<TDocument>>(MockBehavior.Strict);
        cursor.SetupSequence(value => value.MoveNextAsync(CancellationToken.None))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        cursor.Setup(value => value.Current).Returns(values);
        cursor.Setup(value => value.Dispose());
        return cursor;
    }

    private static PassportAuditEvent CreateVisitAuditEvent()
    {
        Visit visit = CreateVisit();
        return PassportVisitAuditEventFactory.VisitCreated(visit, "operation-1");
    }

    private static IReadOnlyCollection<PassportAuditEvent> CreateVisitAuditBatch()
    {
        Visit visit = CreateVisit();
        PassportAuditEvent created =
            PassportVisitAuditEventFactory.VisitCreated(visit, "operation-1");
        visit.Complete(
            new DateOnly(2026, 9, 4),
            NowUtc.AddMinutes(1));
        PassportAuditEvent completed = PassportVisitAuditEventFactory.VisitStatusChanged(
            visit,
            VisitStatus.Draft);
        return new[] { created, completed };
    }

    private static Visit CreateVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 4),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            "Titre privé",
            "Note privée",
            NowUtc);
    }

    private sealed class TestLogger : ILogger<PassportAuditStore>
    {
        public Exception? LastException { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            this.LastException = exception;
        }
    }
}
