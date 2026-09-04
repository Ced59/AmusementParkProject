using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserVisitRepositoryTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateIdempotentAsync_ShouldInsertHashesAndReturnTheCreatedVisit()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        UserVisitDocument? insertedDocument = null;
        collection.Setup(value => value.InsertOneAsync(
                It.IsAny<UserVisitDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback((
                UserVisitDocument document,
                InsertOneOptions _,
                CancellationToken _) => insertedDocument = document)
            .Returns(Task.CompletedTask);
        UserVisitRepository repository = CreateRepository(collection.Object);
        Visit visit = CreateDraftVisit(NowUtc.AddTicks(1234));

        IdempotentVisitCreationResult result = await repository.CreateIdempotentAsync(
            visit,
            " request-1 ",
            CancellationToken.None);

        Assert.NotNull(insertedDocument);
        Assert.Equal("visit-1", insertedDocument.Id);
        Assert.Equal("user-1", insertedDocument.UserId);
        Assert.Equal(64, insertedDocument.CreationOperationKeyHash?.Length);
        Assert.Equal(64, insertedDocument.CreationPayloadHash?.Length);
        Assert.NotNull(insertedDocument.CreationSnapshot);
        Assert.Equal(NowUtc, insertedDocument.CreationSnapshot.CreatedAtUtc);
        Assert.Equal(IdempotentVisitCreationStatus.Created, result.Status);
        Assert.Equal(visit.Id, result.Visit?.Id);
        Assert.Equal(visit.Version, result.Visit?.Version);
        Assert.Equal(NowUtc, result.Visit?.CreatedAtUtc);
        collection.VerifyAll();
    }

    [Fact]
    public async Task TryUpdateOwnedAsync_ShouldUseThePreviousVersionAsAWriteFence()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        FilterDefinition<UserVisitDocument>? capturedFilter = null;
        UpdateDefinition<UserVisitDocument>? capturedUpdate = null;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserVisitDocument> filter,
                UpdateDefinition<UserVisitDocument> update,
                UpdateOptions options,
                CancellationToken _) =>
            {
                capturedFilter = filter;
                capturedUpdate = update;
                Assert.False(options.IsUpsert);
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserVisitRepository repository = CreateRepository(collection.Object);
        Visit visit = CreateDraftVisit();
        visit.UpdateDraft(
            visit.Date,
            visit.TimeZoneId,
            visit.ServiceDayConvention,
            "Titre corrigé",
            visit.PrivateNote,
            NowUtc.AddMinutes(1));

        bool updated = await repository.TryUpdateOwnedAsync(
            visit,
            1,
            CancellationToken.None);

        Assert.True(updated);
        Assert.NotNull(capturedFilter);
        BsonDocument rendered = Render(capturedFilter);
        Assert.Equal("visit-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(1, rendered["version"].AsInt64);
        Assert.NotNull(capturedUpdate);
        BsonDocument renderedUpdate = Render(capturedUpdate);
        Assert.Equal(2, renderedUpdate["$set"].AsBsonDocument["version"].AsInt64);
        Assert.False(renderedUpdate.ToString().Contains("creationOperationKeyHash", StringComparison.Ordinal));
        Assert.False(renderedUpdate.ToString().Contains("creationPayloadHash", StringComparison.Ordinal));
        Assert.False(renderedUpdate.ToString().Contains("creationSnapshot", StringComparison.Ordinal));
        collection.VerifyAll();
    }

    [Fact]
    public async Task TryUpdateOwnedAsync_ShouldReportAConflictWithoutUpserting()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));
        UserVisitRepository repository = CreateRepository(collection.Object);
        Visit visit = CreateDraftVisit();
        visit.UpdateDraft(
            visit.Date,
            visit.TimeZoneId,
            visit.ServiceDayConvention,
            "Titre corrigé",
            visit.PrivateNote,
            NowUtc.AddMinutes(1));

        bool updated = await repository.TryUpdateOwnedAsync(
            visit,
            1,
            CancellationToken.None);

        Assert.False(updated);
        collection.VerifyAll();
    }

    [Fact]
    public async Task TryUpdateOwnedAsync_ShouldWriteTheAssessmentWithTheParentVersionAtomically()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        UpdateDefinition<UserVisitDocument>? capturedUpdate = null;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserVisitDocument> _,
                UpdateDefinition<UserVisitDocument> update,
                UpdateOptions _,
                CancellationToken _) => capturedUpdate = update)
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserVisitRepository repository = CreateRepository(collection.Object);
        Visit visit = CreateDraftVisit();
        visit.UpsertParkAssessment(
            AmusementPark.Core.Domain.Ratings.RatingValue.FromDouble(4.5d),
            "Souvenir privé",
            NowUtc.AddMinutes(1));

        bool updated = await repository.TryUpdateOwnedAsync(visit, 1, CancellationToken.None);

        Assert.True(updated);
        Assert.NotNull(capturedUpdate);
        BsonDocument renderedUpdate = Render(capturedUpdate);
        BsonDocument set = renderedUpdate["$set"].AsBsonDocument;
        Assert.Equal(2, set["version"].AsInt64);
        Assert.Equal(9, set["parkAssessment"]["valueHalfSteps"].AsInt32);
        Assert.Equal(1, set["parkAssessment"]["revision"].AsInt32);
        collection.VerifyAll();
    }

    [Fact]
    public async Task TryUpdateOwnedAuditedAsync_ShouldAtomicallyPushAMinimizedMarker()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        UpdateDefinition<UserVisitDocument>? capturedUpdate = null;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserVisitDocument> _,
                UpdateDefinition<UserVisitDocument> update,
                UpdateOptions _,
                CancellationToken _) => capturedUpdate = update)
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserVisitRepository repository = CreateRepository(collection.Object);
        Visit visit = CreateDraftVisit();
        visit.UpsertParkAssessment(
            AmusementPark.Core.Domain.Ratings.RatingValue.FromDouble(4.5d),
            "Ce texte ne doit pas être copié dans l'audit",
            NowUtc.AddMinutes(1));
        PassportAuditEvent auditEvent =
            PassportVisitAuditEventFactory.ParkAssessmentUpserted(visit, null);

        bool updated = await repository.TryUpdateOwnedAuditedAsync(
            visit,
            1,
            auditEvent,
            CancellationToken.None);

        Assert.True(updated);
        BsonDocument renderedUpdate = Render(capturedUpdate!);
        BsonDocument marker = renderedUpdate["$push"]["pendingAuditEvents"].AsBsonDocument;
        Assert.Equal(auditEvent.Id, marker["eventId"].AsString);
        Assert.Equal(9, marker["newRatingHalfSteps"].AsInt32);
        Assert.False(marker.ToString().Contains("Ce texte", StringComparison.Ordinal));
        Assert.False(marker.Contains("privateComment"));
        collection.VerifyAll();
    }

    [Fact]
    public async Task TryUpdateOwnedAuditedWithinContentMutationLeaseAsync_ShouldFenceByToken()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        FilterDefinition<UserVisitDocument>? capturedFilter = null;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserVisitDocument> filter,
                UpdateDefinition<UserVisitDocument> _,
                UpdateOptions _,
                CancellationToken _) => capturedFilter = filter)
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserVisitRepository repository = CreateRepository(collection.Object);
        Visit visit = CreateDraftVisit();
        visit.UpdateDraft(
            VisitDate.ForYear(2025),
            visit.TimeZoneId,
            visit.ServiceDayConvention,
            visit.Title,
            visit.PrivateNote,
            NowUtc.AddMinutes(1));
        PassportAuditEvent auditEvent =
            PassportVisitAuditEventFactory.VisitUpdated(
                visit,
                new VisitAuditSnapshot(
                    VisitDate.ForDay(2026, 9, 3),
                    "Europe/Paris",
                    LocalServiceDayConvention.VisitStartLocalDate,
                    VisitStatus.Draft,
                    "Titre",
                    "Note privée"));

        bool updated =
            await repository.TryUpdateOwnedAuditedWithinContentMutationLeaseAsync(
                visit,
                1,
                auditEvent,
                "lease-1",
                CancellationToken.None);

        Assert.True(updated);
        Assert.NotNull(capturedFilter);
        BsonDocument rendered = Render(capturedFilter);
        Assert.Equal("visit-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(1, rendered["version"].AsInt64);
        Assert.Equal("lease-1", rendered["contentMutationLeaseToken"].AsString);
        collection.VerifyAll();
    }

    [Fact]
    public async Task TryConfirmOwnedVersionAsync_ShouldFenceANoOpWithoutIncrementingVersion()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        FilterDefinition<UserVisitDocument>? capturedFilter = null;
        UpdateDefinition<UserVisitDocument>? capturedUpdate = null;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserVisitDocument> filter,
                UpdateDefinition<UserVisitDocument> update,
                UpdateOptions options,
                CancellationToken _) =>
            {
                capturedFilter = filter;
                capturedUpdate = update;
                Assert.False(options.IsUpsert);
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 0, null));
        UserVisitRepository repository = CreateRepository(collection.Object);

        bool confirmed = await repository.TryConfirmOwnedVersionAsync(
            VisitId.Parse("visit-1"),
            "user-1",
            3,
            CancellationToken.None);

        Assert.True(confirmed);
        Assert.Equal(3, Render(capturedFilter!)["version"].AsInt64);
        BsonDocument set = Render(capturedUpdate!)["$set"].AsBsonDocument;
        Assert.Equal(3, set["version"].AsInt64);
        Assert.Single(set);
        collection.VerifyAll();
    }

    [Fact]
    public async Task ListOwnedAsync_ShouldRejectAnUnboundedRequestBeforeMongo()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        UserVisitRepository repository = CreateRepository(collection.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.ListOwnedAsync(
                new UserVisitListCriteria(
                    "user-1",
                    UserVisitRepository.MaximumListSize + 1),
                CancellationToken.None));
    }

    [Fact]
    public void ResolveIdempotentCreation_WhenPayloadMatches_ShouldReplayTheExistingVisit()
    {
        Visit visit = CreateDraftVisit();
        UserVisitDocument document = visit.ToDocument();
        document.CreationPayloadHash = UserVisitCreationFingerprint.HashPayload(visit);
        document.CreationSnapshot = document.CreateCreationSnapshot();
        document.Title = "Titre modifié";
        document.Status = VisitStatus.Completed;
        document.Version = 2;
        document.UpdatedAt = NowUtc.AddHours(2);
        document.CompletedAtUtc = NowUtc.AddHours(2);

        IdempotentVisitCreationResult result = UserVisitRepository.ResolveIdempotentCreation(
            document,
            document.CreationPayloadHash);

        Assert.Equal(IdempotentVisitCreationStatus.Replayed, result.Status);
        Assert.Equal(visit.Id, result.Visit?.Id);
        Assert.Equal("Titre", result.Visit?.Title);
        Assert.Equal(VisitStatus.Draft, result.Visit?.Status);
        Assert.Equal(1, result.Visit?.Version);
        Assert.Equal(NowUtc, result.Visit?.UpdatedAtUtc);
    }

    [Fact]
    public void ResolveIdempotentCreation_WhenPayloadDiffers_ShouldReturnAConflict()
    {
        UserVisitDocument document = CreateDraftVisit().ToDocument();
        document.CreationPayloadHash = "first-payload";

        IdempotentVisitCreationResult result = UserVisitRepository.ResolveIdempotentCreation(
            document,
            "second-payload");

        Assert.Equal(IdempotentVisitCreationStatus.Conflict, result.Status);
        Assert.Null(result.Visit);
    }

    private static UserVisitRepository CreateRepository(
        IMongoCollection<UserVisitDocument> collection)
    {
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<UserVisitDocument>("user-visits", null))
            .Returns(collection);
        return new UserVisitRepository(
            database.Object,
            new MongoDbSettings { UserVisitsCollectionName = "user-visits" });
    }

    private static Visit CreateDraftVisit(DateTime? createdAtUtc = null)
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            "Titre",
            "Note privée",
            createdAtUtc ?? NowUtc);
    }

    private static BsonDocument Render(
        FilterDefinition<UserVisitDocument> filter)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        RenderArgs<UserVisitDocument> arguments =
            new RenderArgs<UserVisitDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(
        UpdateDefinition<UserVisitDocument> update)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        RenderArgs<UserVisitDocument> arguments =
            new RenderArgs<UserVisitDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return update.Render(arguments).AsBsonDocument;
    }
}
