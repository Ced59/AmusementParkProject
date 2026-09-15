using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class FactualEventMongoMapperTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void OutboxMapping_ShouldRoundTripStructuredEvidenceAndRevision()
    {
        FactualChangeOutboxEntry entry = CreateOutboxEntry();

        FactualChangeOutboxDocument document = entry.ToDocument();
        FactualChangeOutboxEntry restored = document.ToDomain();

        Assert.Equal(entry, restored);
        Assert.Equal(FactValueKind.Money, document.NewValue?.Kind);
        Assert.Equal("EUR", document.NewValue?.UnitCode);
    }

    [Fact]
    public void EventMapping_ShouldRoundTripHistoricalDefinitionAndLifecycle()
    {
        FactualChangeEvent factualEvent = FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse("event-1"),
            FactualEventType.TicketPricePublishedOrChanged,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromMoney(49.90m, "EUR"),
            FactValue.FromMoney(51.50m, "EUR"),
            CreateSource(),
            DataConfidence.High,
            NowUtc.AddHours(-1),
            "park:park-1:ticket-price",
            7,
            NowUtc);

        FactualChangeEvent restored = factualEvent.ToDocument().ToDomain();

        Assert.Equal(factualEvent.Id, restored.Id);
        Assert.Equal(factualEvent.DefinitionVersion, restored.DefinitionVersion);
        Assert.Equal(factualEvent.PreviousValue, restored.PreviousValue);
        Assert.Equal(factualEvent.NewValue, restored.NewValue);
        Assert.Equal(factualEvent.Source, restored.Source);
        Assert.Equal(factualEvent.Status, restored.Status);
        Assert.Equal(factualEvent.Version, restored.Version);
    }

    [Fact]
    public void OutboxMapping_WithSubMillisecondDates_ShouldCanonicalizeToBsonPrecision()
    {
        DateTime recordedAtUtc = NowUtc.AddTicks(7);
        DateTime occurredAtUtc = NowUtc.AddHours(-1).AddTicks(6);
        DateTime publishedAtUtc = NowUtc.AddHours(-2).AddTicks(5);
        FactualChangeOutboxEntry entry = new FactualChangeOutboxEntry(
            "outbox-1",
            "event-1",
            FactualEventType.ParkNameChanged,
            FactualEventCatalog.CurrentSchemaVersion,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Ancien nom"),
            FactValue.FromText("Nouveau nom"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Nom officiel",
                "https://example.com/news",
                publishedAtUtc),
            DataConfidence.High,
            occurredAtUtc,
            "park:park-1:name",
            7,
            recordedAtUtc,
            null,
            1);

        FactualChangeOutboxEntry restored = entry.ToDocument().ToDomain();

        Assert.Equal(0, restored.RecordedAtUtc.Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(0, restored.OccurredAtUtc.Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(0, restored.Source.PublishedAtUtc.Ticks % TimeSpan.TicksPerMillisecond);
    }

    private static FactualChangeOutboxEntry CreateOutboxEntry()
    {
        return new FactualChangeOutboxEntry(
            "outbox-1",
            "event-1",
            FactualEventType.TicketPricePublishedOrChanged,
            FactualEventCatalog.CurrentSchemaVersion,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromMoney(49.90m, "EUR"),
            FactValue.FromMoney(51.50m, "EUR"),
            CreateSource(),
            DataConfidence.High,
            NowUtc.AddHours(-1),
            "park:park-1:ticket-price",
            7,
            NowUtc,
            null,
            1);
    }

    private static SourceReference CreateSource()
    {
        return new SourceReference(
            SourceReferenceType.OfficialWebsite,
            "Parc exemple",
            "Tarifs officiels",
            "https://example.com/prices",
            NowUtc.AddHours(-2));
    }
}
