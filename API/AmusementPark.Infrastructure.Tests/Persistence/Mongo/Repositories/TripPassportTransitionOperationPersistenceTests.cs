using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripPassportTransitionOperationPersistenceTests
{
    [Fact]
    public async Task CompleteEmptyBatchCreationOperationAsync_ShouldPersistACompletedMarker()
    {
        UserRideOccurrenceCreationOperationDocument? inserted = null;
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operations = new();
        operations.Setup(collection => collection.InsertOneAsync(
                It.IsAny<UserRideOccurrenceCreationOperationDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback<UserRideOccurrenceCreationOperationDocument, InsertOneOptions, CancellationToken>(
                (document, _, _) => inserted = document)
            .Returns(Task.CompletedTask);
        UserRideOccurrenceRepository repository = CreateRepository(operations.Object);
        VisitId visitId = VisitId.New();
        DateTime completedAtUtc = new(2027, 8, 22, 10, 0, 0, DateTimeKind.Utc);

        bool completed = await repository.CompleteEmptyBatchCreationOperationAsync(
            "user-1",
            visitId,
            "trip-passport-rides:test",
            completedAtUtc,
            CancellationToken.None);

        Assert.True(completed);
        Assert.NotNull(inserted);
        Assert.Equal("user-1", inserted.UserId);
        Assert.Equal(visitId.Value, inserted.VisitId);
        Assert.Equal("creation", inserted.OperationKind);
        Assert.Equal("completed", inserted.OperationState);
        Assert.Empty(inserted.Items);
        Assert.Equal(completedAtUtc, inserted.CreatedAt);
        operations.VerifyAll();
    }

    [Fact]
    public async Task ReserveBatchCreationKeyAsync_ShouldPersistTheOrderedParkItemIds()
    {
        UserRideOccurrenceCreationOperationDocument? inserted = null;
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operations = new();
        operations.Setup(collection => collection.InsertOneAsync(
                It.IsAny<UserRideOccurrenceCreationOperationDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback<UserRideOccurrenceCreationOperationDocument, InsertOneOptions, CancellationToken>(
                (document, _, _) => inserted = document)
            .Returns(Task.CompletedTask);
        UserRideOccurrenceRepository repository = CreateRepository(operations.Object);
        VisitId visitId = VisitId.New();
        RideOccurrenceCreationRequest request = new(
            visitId,
            "user-1",
            new[]
            {
                new RideOccurrenceCreationRequestItem(
                    "item-1",
                    new OccurrenceMoment(null, true),
                    RideOccurrenceStatus.Completed,
                    RideLogSource.TripTransition,
                    null,
                    true),
                new RideOccurrenceCreationRequestItem(
                    "item-2",
                    new OccurrenceMoment(null, true),
                    RideOccurrenceStatus.Completed,
                    RideLogSource.TripTransition,
                    null,
                    true),
            });
        RideOccurrenceCreationPreparation preparation = new(
            "park-1",
            VisitDate.ForDay(2027, 8, 20),
            "Europe/Paris",
            LocalServiceDayConvention.UserSelectedServiceDate,
            new[] { HistoricalConsistency.Verified, HistoricalConsistency.Unverified });

        RideOccurrenceCreationKeyReservationResult result =
            await repository.ReserveBatchCreationKeyAsync(
                request,
                preparation,
                "trip-passport-rides:test",
                new DateTime(2027, 8, 22, 10, 0, 0, DateTimeKind.Utc),
                CancellationToken.None);

        Assert.Equal(RideOccurrenceCreationKeyReservationStatus.Reserved, result.Status);
        Assert.NotNull(inserted?.CreationPreparation);
        Assert.Equal(
            new[] { "item-1", "item-2" },
            inserted.CreationPreparation.Items
                .OrderBy(static item => item.Index)
                .Select(static item => item.ParkItemId));
        operations.VerifyAll();
    }

    private static UserRideOccurrenceRepository CreateRepository(
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> operations)
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> occurrences = new();
        return new UserRideOccurrenceRepository(occurrences.Object, operations);
    }
}
