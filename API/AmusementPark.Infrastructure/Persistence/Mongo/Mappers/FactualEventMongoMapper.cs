using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class FactualEventMongoMapper
{
    public static FactualChangeOutboxDocument ToDocument(this FactualChangeOutboxEntry entry)
    {
        return new FactualChangeOutboxDocument
        {
            Id = entry.Id,
            EventId = entry.EventId,
            Type = entry.Type,
            DefinitionVersion = entry.DefinitionVersion,
            Target = ToDocument(entry.Target),
            PreviousValue = ToDocument(entry.PreviousValue),
            NewValue = ToDocument(entry.NewValue),
            Source = ToDocument(entry.Source),
            Confidence = entry.Confidence,
            OccurredAtUtc = NormalizeToBsonPrecision(entry.OccurredAtUtc),
            DeduplicationKey = entry.DeduplicationKey,
            SourceRevision = entry.SourceRevision,
            MaterializedAtUtc = NormalizeToBsonPrecision(entry.MaterializedAtUtc),
            TerminalAtUtc = NormalizeToBsonPrecision(entry.TerminalAtUtc),
            TerminalErrorCode = entry.TerminalErrorCode,
            Version = entry.Version,
            CreatedAt = NormalizeToBsonPrecision(entry.RecordedAtUtc),
            UpdatedAt = NormalizeToBsonPrecision(
                entry.MaterializedAtUtc ?? entry.RecordedAtUtc),
        };
    }

    public static FactualChangeOutboxEntry ToDomain(this FactualChangeOutboxDocument document)
    {
        return new FactualChangeOutboxEntry(
            document.Id,
            document.EventId,
            document.Type,
            document.DefinitionVersion,
            ToDomain(document.Target),
            ToDomain(document.PreviousValue),
            ToDomain(document.NewValue),
            ToDomain(document.Source),
            document.Confidence,
            document.OccurredAtUtc,
            document.DeduplicationKey,
            document.SourceRevision,
            document.CreatedAt,
            document.MaterializedAtUtc,
            document.TerminalAtUtc,
            document.TerminalErrorCode,
            document.Version);
    }

    public static FactualChangeEventDocument ToDocument(this FactualChangeEvent factualEvent)
    {
        return new FactualChangeEventDocument
        {
            Id = factualEvent.Id.Value,
            Type = factualEvent.Type,
            DefinitionVersion = factualEvent.DefinitionVersion,
            Target = ToDocument(factualEvent.Target),
            PreviousValue = ToDocument(factualEvent.PreviousValue),
            NewValue = ToDocument(factualEvent.NewValue),
            Source = ToDocument(factualEvent.Source),
            Confidence = factualEvent.Confidence,
            OccurredAtUtc = NormalizeToBsonPrecision(factualEvent.OccurredAtUtc),
            DeduplicationKey = factualEvent.DeduplicationKey,
            Revision = factualEvent.Revision,
            Status = factualEvent.Status,
            VerifiedAtUtc = NormalizeToBsonPrecision(factualEvent.VerifiedAtUtc),
            PublishedAtUtc = NormalizeToBsonPrecision(factualEvent.PublishedAtUtc),
            TerminalAtUtc = NormalizeToBsonPrecision(factualEvent.TerminalAtUtc),
            SupersededByEventId = factualEvent.SupersededByEventId?.Value,
            ReasonCode = factualEvent.ReasonCode,
            Version = factualEvent.Version,
            CreatedAt = NormalizeToBsonPrecision(factualEvent.CreatedAtUtc),
            UpdatedAt = NormalizeToBsonPrecision(factualEvent.UpdatedAtUtc),
        };
    }

    public static FactualChangeEvent ToDomain(this FactualChangeEventDocument document)
    {
        FactualChangeEventId? supersededByEventId = string.IsNullOrWhiteSpace(document.SupersededByEventId)
            ? null
            : FactualChangeEventId.Parse(document.SupersededByEventId);
        return FactualChangeEvent.Restore(
            FactualChangeEventId.Parse(document.Id),
            document.Type,
            document.DefinitionVersion,
            ToDomain(document.Target),
            ToDomain(document.PreviousValue),
            ToDomain(document.NewValue),
            ToDomain(document.Source),
            document.Confidence,
            document.OccurredAtUtc,
            document.DeduplicationKey,
            document.Revision,
            document.Status,
            document.CreatedAt,
            document.UpdatedAt,
            document.VerifiedAtUtc,
            document.PublishedAtUtc,
            document.TerminalAtUtc,
            supersededByEventId,
            document.ReasonCode,
            document.Version);
    }

    private static ChangeTargetDocument ToDocument(ChangeTarget target)
    {
        return new ChangeTargetDocument
        {
            Type = target.Type,
            TargetId = target.TargetId,
            ParentParkId = target.ParentParkId,
        };
    }

    private static ChangeTarget ToDomain(ChangeTargetDocument document)
    {
        return ChangeTarget.Restore(document.Type, document.TargetId, document.ParentParkId);
    }

    private static FactValueDocument? ToDocument(FactValue? value)
    {
        return value is null
            ? null
            : new FactValueDocument
            {
                Kind = value.Kind,
                CanonicalValue = value.CanonicalValue,
                UnitCode = value.UnitCode,
            };
    }

    private static FactValue? ToDomain(FactValueDocument? document)
    {
        return document is null
            ? null
            : FactValue.Restore(document.Kind, document.CanonicalValue, document.UnitCode);
    }

    private static SourceReferenceDocument ToDocument(SourceReference source)
    {
        return new SourceReferenceDocument
        {
            Type = source.Type,
            PublisherName = source.PublisherName,
            Title = source.Title,
            Url = source.Url,
            PublishedAtUtc = NormalizeToBsonPrecision(source.PublishedAtUtc),
        };
    }

    private static SourceReference ToDomain(SourceReferenceDocument document)
    {
        return new SourceReference(
            document.Type,
            document.PublisherName,
            document.Title,
            document.Url,
            document.PublishedAtUtc);
    }

    private static DateTime NormalizeToBsonPrecision(DateTime value)
    {
        long normalizedTicks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(normalizedTicks, value.Kind);
    }

    private static DateTime? NormalizeToBsonPrecision(DateTime? value)
    {
        return value.HasValue ? NormalizeToBsonPrecision(value.Value) : null;
    }
}
