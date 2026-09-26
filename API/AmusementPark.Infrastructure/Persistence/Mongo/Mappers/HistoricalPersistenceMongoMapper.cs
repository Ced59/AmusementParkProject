using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class HistoricalPersistenceMongoMapper
{
    public static HistoricalFactDocument ToDocument(
        this HistoricalFact fact,
        HistoricalReviewEvent transitionReviewEvent)
    {
        ArgumentNullException.ThrowIfNull(fact);
        ArgumentNullException.ThrowIfNull(transitionReviewEvent);
        DateTime recordedAtUtc = NormalizeToBsonPrecision(fact.RecordedAtUtc);
        return new HistoricalFactDocument
        {
            Id = BuildRevisionDocumentId(fact.Id, fact.Revision),
            FactId = fact.Id.ToString("N", CultureInfo.InvariantCulture),
            Revision = fact.Revision,
            RevisionOrigin = fact.RevisionOrigin,
            SupersedesRevision = fact.SupersedesRevision,
            Subject = ToDocument(fact.Subject),
            Type = fact.Type,
            Period = ToDocument(fact.Period),
            State = fact.State,
            Importance = fact.Importance,
            WorkflowState = fact.WorkflowState,
            PublicationState = fact.PublicationState,
            PublicUncertaintyExplanation = fact.PublicUncertaintyExplanation
                .Select(static explanation => new LocalizedTextDocument
                {
                    LanguageCode = explanation.LanguageCode,
                    Value = explanation.Value,
                })
                .ToList(),
            LifecycleBoundaryMeaning = fact.LifecycleBoundaryMeaning,
            AttributeKind = fact.AttributeKind,
            AttributeBoundaryMeaning = fact.AttributeBoundaryMeaning,
            SequenceWithinDate = fact.SequenceWithinDate,
            Sources = fact.SourceReferences
                .Select(static sourceReference => new HistoricalSourceRevisionDocument
                {
                    SourceId = sourceReference.SourceId.ToString("N", CultureInfo.InvariantCulture),
                    Revision = sourceReference.Revision,
                    SubjectType = sourceReference.SubjectType,
                    SubjectId = sourceReference.SubjectId,
                    FactType = sourceReference.FactType,
                    Period = ToDocument(sourceReference.Period),
                    Position = sourceReference.Position,
                    Scopes = sourceReference.Scopes.ToList(),
                })
                .ToList(),
            StructuredValue = fact.StructuredValue,
            OtherTypeLabel = fact.OtherTypeLabel,
            NarrativeContentId = fact.NarrativeContentId,
            VerifiedAtUtc = NormalizeToBsonPrecision(fact.VerifiedAtUtc),
            PublishedAtUtc = NormalizeToBsonPrecision(fact.PublishedAtUtc),
            PublicationMethodologyVersion = fact.PublicationMethodologyVersion,
            TransitionReviewEvent = transitionReviewEvent.ToDocument(),
            CreatedAt = recordedAtUtc,
            UpdatedAt = recordedAtUtc,
        };
    }

    public static HistoricalFact ToDomain(this HistoricalFactDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new HistoricalFact(
            ParseGuid(document.FactId),
            ToDomain(document.Subject),
            document.Type,
            ToDomain(document.Period),
            document.State,
            document.Importance,
            document.WorkflowState,
            document.PublicationState,
            document.PublicUncertaintyExplanation
                .Select(static explanation => new HistoricalLocalizedText(
                    explanation.LanguageCode,
                    explanation.Value ?? string.Empty))
                .ToArray(),
            document.LifecycleBoundaryMeaning,
            document.AttributeKind,
            document.AttributeBoundaryMeaning,
            document.SequenceWithinDate,
            document.Sources
                .Select(static source => new HistoricalSourceRevisionReference(
                    ParseGuid(source.SourceId),
                    source.Revision,
                    source.SubjectType,
                    source.SubjectId,
                    source.FactType,
                    ToDomain(source.Period),
                    source.Position,
                    source.Scopes))
                .ToArray(),
            document.StructuredValue,
            document.OtherTypeLabel,
            document.NarrativeContentId,
            NormalizeToBsonPrecision(document.VerifiedAtUtc),
            NormalizeToBsonPrecision(document.PublishedAtUtc),
            document.PublicationMethodologyVersion,
            document.Revision,
            document.SupersedesRevision,
            NormalizeToBsonPrecision(document.CreatedAt),
            document.RevisionOrigin);
    }

    public static HistoricalSourceDocument ToDocument(
        this HistoricalSourceReference source,
        HistoricalReviewEvent transitionReviewEvent)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(transitionReviewEvent);
        DateTime recordedAtUtc = NormalizeToBsonPrecision(source.RecordedAtUtc);
        return new HistoricalSourceDocument
        {
            Id = BuildRevisionDocumentId(source.Id, source.Revision),
            SourceId = source.Id.ToString("N", CultureInfo.InvariantCulture),
            Revision = source.Revision,
            RevisionOrigin = source.RevisionOrigin,
            Type = source.Type,
            Title = source.Title,
            PublisherOrAuthor = source.PublisherOrAuthor,
            Url = source.Url,
            BibliographicReference = source.BibliographicReference,
            PublishedOn = FormatDate(source.PublishedOn),
            AccessedOn = FormatDate(source.AccessedOn)!,
            LanguageCode = source.LanguageCode,
            ArchiveUrl = source.ArchiveUrl,
            Scopes = source.Scopes.ToList(),
            AdminNote = source.AdminNote,
            Accessibility = source.Accessibility,
            WorkflowState = source.WorkflowState,
            PublicationState = source.PublicationState,
            TransitionReviewEvent = transitionReviewEvent.ToDocument(),
            CreatedAt = recordedAtUtc,
            UpdatedAt = recordedAtUtc,
        };
    }

    public static HistoricalSourceReference ToDomain(this HistoricalSourceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new HistoricalSourceReference(
            ParseGuid(document.SourceId),
            document.Revision,
            document.Type,
            document.Title,
            document.PublisherOrAuthor,
            document.Url,
            document.BibliographicReference,
            ParseOptionalDate(document.PublishedOn),
            ParseRequiredDate(document.AccessedOn),
            document.LanguageCode,
            document.ArchiveUrl,
            document.Scopes,
            document.AdminNote,
            document.Accessibility,
            document.WorkflowState,
            document.PublicationState,
            NormalizeToBsonPrecision(document.CreatedAt),
            document.RevisionOrigin);
    }

    public static HistoricalReviewEventDocument ToDocument(this HistoricalReviewEvent reviewEvent)
    {
        ArgumentNullException.ThrowIfNull(reviewEvent);
        DateTime occurredAtUtc = NormalizeToBsonPrecision(reviewEvent.OccurredAtUtc);
        return new HistoricalReviewEventDocument
        {
            Id = reviewEvent.Id.ToString("N", CultureInfo.InvariantCulture),
            ResourceType = reviewEvent.ResourceType,
            ResourceId = reviewEvent.ResourceId.ToString("N", CultureInfo.InvariantCulture),
            ResourceRevision = reviewEvent.ResourceRevision,
            EventType = reviewEvent.EventType,
            ActorUserId = reviewEvent.ActorUserId,
            PrivateNote = reviewEvent.PrivateNote,
            OccurredAtUtc = occurredAtUtc,
            CreatedAt = occurredAtUtc,
            UpdatedAt = occurredAtUtc,
        };
    }

    public static HistoricalReviewEvent ToDomain(this HistoricalReviewEventDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new HistoricalReviewEvent(
            ParseGuid(document.Id),
            document.ResourceType,
            ParseGuid(document.ResourceId),
            document.ResourceRevision,
            document.EventType,
            document.ActorUserId,
            document.PrivateNote,
            NormalizeToBsonPrecision(document.OccurredAtUtc));
    }

    private static HistoricalSubjectDocument ToDocument(HistoricalSubject subject)
    {
        return new HistoricalSubjectDocument
        {
            Type = subject.Type,
            Id = subject.Id,
            HistoricalLabel = subject.HistoricalLabel,
            PublicationPolicy = subject.PublicationPolicy,
        };
    }

    private static HistoricalSubject ToDomain(HistoricalSubjectDocument document)
    {
        return new HistoricalSubject(
            document.Type,
            document.Id,
            document.HistoricalLabel,
            document.PublicationPolicy);
    }

    private static HistoricalPeriodDocument ToDocument(HistoricalPeriod period)
    {
        return new HistoricalPeriodDocument
        {
            Start = ToDocument(period.Start),
            End = ToDocument(period.End),
            StartConfidence = period.StartConfidence,
            EndConfidence = period.EndConfidence,
        };
    }

    private static HistoricalPeriod ToDomain(HistoricalPeriodDocument document)
    {
        return new HistoricalPeriod(
            ToDomain(document.Start),
            ToDomain(document.End),
            document.StartConfidence,
            document.EndConfidence);
    }

    private static HistoricalDateDocument? ToDocument(HistoricalDate? date)
    {
        return date is null
            ? null
            : new HistoricalDateDocument
            {
                Year = date.Year,
                Month = date.Month,
                Day = date.Day,
                Precision = date.Precision,
                IsApproximate = date.IsApproximate,
                Qualifier = date.Qualifier,
            };
    }

    private static HistoricalDate? ToDomain(HistoricalDateDocument? document)
    {
        return document is null
            ? null
            : new HistoricalDate(
                document.Year,
                document.Month,
                document.Day,
                document.Precision,
                document.IsApproximate,
                document.Qualifier);
    }

    private static string BuildRevisionDocumentId(Guid resourceId, int revision)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{resourceId:N}:{revision:D10}");
    }

    private static Guid ParseGuid(string value)
    {
        return Guid.ParseExact(value, "N");
    }

    private static string? FormatDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static DateOnly ParseRequiredDate(string value)
    {
        return DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static DateOnly? ParseOptionalDate(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseRequiredDate(value);
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
