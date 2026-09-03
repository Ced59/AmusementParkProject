using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class UserRideOccurrenceMongoMapperTests
{
    private static readonly DateTime CreatedAtUtc =
        new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Mapper_ShouldRoundTripThePrivateOccurrenceWithoutLosingLocalTime()
    {
        RideOccurrence occurrence = CreateOccurrence(CreatedAtUtc.AddTicks(4321));
        occurrence.Update(
            CreateVisit(),
            new OccurrenceMoment(new TimeOnly(14, 25, 30), true),
            RideOccurrenceStatus.Attempted,
            HistoricalConsistency.Unverified,
            new HistoricalTargetReference("Nom historique", "Attraction"),
            "Évacuation avant le départ.",
            CreatedAtUtc.AddMinutes(5).AddTicks(9876));

        UserRideOccurrenceDocument document = occurrence.ToDocument();
        RideOccurrence restored = document.ToDomain();

        Assert.Equal(occurrence.Id, restored.Id);
        Assert.Equal(occurrence.VisitId, restored.VisitId);
        Assert.Equal(occurrence.UserId, restored.UserId);
        Assert.Equal(occurrence.ParkId, restored.ParkId);
        Assert.Equal(occurrence.ParkItemId, restored.ParkItemId);
        Assert.Equal(occurrence.SortPosition, restored.SortPosition);
        Assert.Equal(occurrence.Moment, restored.Moment);
        Assert.Equal(occurrence.Status, restored.Status);
        Assert.Equal(occurrence.Source, restored.Source);
        Assert.Equal(occurrence.HistoricalConsistency, restored.HistoricalConsistency);
        Assert.Equal(occurrence.HistoricalTarget, restored.HistoricalTarget);
        Assert.Equal(occurrence.PrivateNote, restored.PrivateNote);
        Assert.Equal(occurrence.Version, restored.Version);
        Assert.Equal(CreatedAtUtc, restored.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc.AddMinutes(5), restored.UpdatedAtUtc);
    }

    [Fact]
    public void DocumentSerialization_ShouldUseStableFieldsAndStringEnums()
    {
        RideOccurrence occurrence = CreateOccurrence();

        BsonDocument serialized = occurrence.ToDocument().ToBsonDocument();

        Assert.Equal("occurrence-1", serialized["_id"].AsString);
        Assert.Equal(1, serialized["schemaVersion"].AsInt32);
        Assert.Equal("visit-1", serialized["visitId"].AsString);
        Assert.Equal("user-1", serialized["userId"].AsString);
        Assert.Equal("park-1", serialized["parkId"].AsString);
        Assert.Equal("item-1", serialized["parkItemId"].AsString);
        Assert.Equal(1024, serialized["sortPosition"].AsInt64);
        Assert.Equal("Completed", serialized["status"].AsString);
        Assert.Equal("Manual", serialized["source"].AsString);
        Assert.Equal("Verified", serialized["historicalConsistency"].AsString);
        Assert.False(serialized["moment"].AsBsonDocument.Contains("localTime"));
        Assert.False(serialized.Contains("historicalTarget"));
        Assert.False(serialized.Contains("privateNote"));
        Assert.False(serialized.Contains("deletedAtUtc"));
        Assert.False(serialized.Contains("creationOperationKeyHash"));
        Assert.False(serialized.Contains("creationSnapshot"));
    }

    [Fact]
    public void CreationSnapshot_ShouldRemainStableAfterTheLiveDocumentChanges()
    {
        UserRideOccurrenceDocument document = CreateOccurrence().ToDocument();
        document.CreationSnapshot = document.CreateCreationSnapshot();
        document.Status = RideOccurrenceStatus.MissedClosed;
        document.PrivateNote = "État modifié";
        document.Version = 2;
        document.UpdatedAt = CreatedAtUtc.AddHours(1);

        RideOccurrence replayed = document.CreationSnapshotToDomain();

        Assert.Equal(RideOccurrenceStatus.Completed, replayed.Status);
        Assert.Null(replayed.PrivateNote);
        Assert.Equal(1, replayed.Version);
        Assert.Equal(CreatedAtUtc, replayed.UpdatedAtUtc);
    }

    private static RideOccurrence CreateOccurrence(DateTime? createdAtUtc = null)
    {
        return RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-1"),
            CreateVisit(createdAtUtc),
            "item-1",
            1024,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            createdAtUtc ?? CreatedAtUtc);
    }

    private static Visit CreateVisit(DateTime? createdAtUtc = null)
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            createdAtUtc ?? CreatedAtUtc);
    }
}
