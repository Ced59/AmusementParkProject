using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.ParkOpeningHours.Models;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkOpeningHoursFactualMarkerPersistenceTests
{
    [Fact]
    public void ScheduleDocument_WithPendingFact_ShouldRoundTripDurableMarker()
    {
        DateTime recordedAtUtc = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        ParkOpeningHoursFactualChangeDraft draft = new ParkOpeningHoursFactualChangeDraft(
            FactualEventType.OpeningCalendarPublished,
            ChangeTarget.ForPark("park-1"),
            null,
            FactValue.FromText("calendar"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Park",
                "Calendar",
                "https://example.com/calendar",
                recordedAtUtc),
            DataConfidence.High,
            recordedAtUtc,
            "park:park-1:opening-calendar");
        FactualChangeOutboxEntry entry = FactualChangeOutboxEntry.Create(
            draft.ToCaptureRequest(7, recordedAtUtc))!;
        ParkOpeningHoursScheduleDocument source = new ParkOpeningHoursScheduleDocument
        {
            Id = "schedule-1",
            ParkId = "park-1",
            TimeZoneId = "Europe/Paris",
            FactualRevision = 7,
            PendingFactualChanges = new List<FactualChangeOutboxDocument>
            {
                entry.ToDocument(),
            },
        };

        BsonDocument bson = source.ToBsonDocument();
        ParkOpeningHoursScheduleDocument restored =
            BsonSerializer.Deserialize<ParkOpeningHoursScheduleDocument>(bson);

        Assert.Equal(7, restored.FactualRevision);
        Assert.Equal(entry, Assert.Single(restored.PendingFactualChanges).ToDomain());
    }

    [Fact]
    public void PendingFilter_ShouldSelectOnlySchedulesWithDurableMarkers()
    {
        BsonDocument rendered = Render(
            ParkOpeningHoursRepository.BuildPendingFactualChangeFilter());

        Assert.Equal(
            new BsonDocument(
                "pendingFactualChanges.0",
                new BsonDocument("$exists", true)),
            rendered);
    }

    [Fact]
    public void RecordedUpdate_ShouldRemoveOnlyTheAcknowledgedMarker()
    {
        UpdateDefinition<ParkOpeningHoursScheduleDocument> update =
            ParkOpeningHoursRepository.BuildMarkFactualChangeRecordedUpdate("outbox-7");
        IBsonSerializer<ParkOpeningHoursScheduleDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkOpeningHoursScheduleDocument>();
        RenderArgs<ParkOpeningHoursScheduleDocument> arguments =
            new RenderArgs<ParkOpeningHoursScheduleDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);

        string json = update.Render(arguments).ToJson();

        Assert.Contains("$pull", json, StringComparison.Ordinal);
        Assert.Contains("outbox-7", json, StringComparison.Ordinal);
    }

    private static BsonDocument Render(
        FilterDefinition<ParkOpeningHoursScheduleDocument> filter)
    {
        IBsonSerializer<ParkOpeningHoursScheduleDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkOpeningHoursScheduleDocument>();
        RenderArgs<ParkOpeningHoursScheduleDocument> arguments =
            new RenderArgs<ParkOpeningHoursScheduleDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
