using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class HistoricalPersistenceMongoMapperTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc).AddTicks(9876);

    [Fact]
    public void FactMapping_ShouldRoundTripCanonicalRevisionAtBsonPrecision()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12));
        HistoricalFact fact = new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc historique",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            HistoricalFactType.Opening,
            period,
            HistoricalFactState.Verified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            1,
            new[]
            {
                new HistoricalSourceRevisionReference(
                    sourceId,
                    4,
                    HistoricalSubjectType.Park,
                    "park-1",
                    HistoricalFactType.Opening,
                    period,
                    HistoricalEvidencePosition.Supports,
                    new[]
                    {
                        HistoricalSourceScope.SubjectIdentity,
                        HistoricalSourceScope.HistoricalLabel,
                        HistoricalSourceScope.FactType,
                        HistoricalSourceScope.Period,
                        HistoricalSourceScope.SequenceWithinDate,
                    }),
            },
            null,
            null,
            "opening-1998",
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            2,
            1,
            RecordedAtUtc);

        HistoricalReviewEvent reviewEvent = CreateReviewEvent(
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            HistoricalReviewEventType.Published);
        HistoricalFactDocument document = fact.ToDocument(reviewEvent);
        HistoricalFact restored = document.ToDomain();

        Assert.Equal(fact.Id, restored.Id);
        Assert.Equal(2, restored.Revision);
        Assert.Equal(1, restored.SupersedesRevision);
        Assert.Equal(HistoricalRevisionOrigin.Ordinary, restored.RevisionOrigin);
        HistoricalSourceRevisionReference sourceReference = Assert.Single(restored.SourceReferences);
        Assert.Equal(sourceId, sourceReference.SourceId);
        Assert.Equal(4, sourceReference.Revision);
        Assert.Equal(HistoricalSubjectType.Park, sourceReference.SubjectType);
        Assert.Equal("park-1", sourceReference.SubjectId);
        Assert.Equal(HistoricalFactType.Opening, sourceReference.FactType);
        Assert.Equal(period, sourceReference.Period);
        Assert.Equal(HistoricalEvidencePosition.Supports, sourceReference.Position);
        Assert.Equal(5, sourceReference.Scopes.Count);
        Assert.Equal("park-1", restored.Subject.Id);
        Assert.Equal(0, restored.RecordedAtUtc.Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(0, restored.VerifiedAtUtc?.Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(0, restored.PublishedAtUtc?.Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(reviewEvent.Id, document.TransitionReviewEvent.ToDomain().Id);
    }

    [Fact]
    public void SourceMapping_ShouldRoundTripEvidenceMetadata()
    {
        HistoricalSourceReference source = new HistoricalSourceReference(
            Guid.NewGuid(),
            3,
            HistoricalSourceType.Archive,
            "Plan officiel",
            "Parc historique",
            "https://example.com/plan",
            null,
            new DateOnly(1998, 5, 1),
            new DateOnly(2026, 9, 25),
            "fr",
            "https://web.archive.org/plan",
            new[] { HistoricalSourceScope.SubjectIdentity, HistoricalSourceScope.Period },
            "Document relu.",
            HistoricalSourceAccessibility.Archived,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            RecordedAtUtc);

        HistoricalReviewEvent reviewEvent = CreateReviewEvent(
            HistoricalReviewResourceType.Source,
            source.Id,
            source.Revision,
            HistoricalReviewEventType.Published);
        HistoricalSourceDocument document = source.ToDocument(reviewEvent);
        HistoricalSourceReference restored = document.ToDomain();

        Assert.Equal(source.Id, restored.Id);
        Assert.Equal(source.Revision, restored.Revision);
        Assert.Equal(HistoricalRevisionOrigin.Ordinary, restored.RevisionOrigin);
        Assert.Equal(source.PublishedOn, restored.PublishedOn);
        Assert.Equal(source.AccessedOn, restored.AccessedOn);
        Assert.Equal(source.Scopes, restored.Scopes);
        Assert.Equal(0, restored.RecordedAtUtc.Ticks % TimeSpan.TicksPerMillisecond);
        Assert.Equal(reviewEvent.Id, document.TransitionReviewEvent.ToDomain().Id);
    }

    [Fact]
    public void ReviewEventMapping_ShouldRoundTripImmutableAuditEntry()
    {
        HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
            Guid.NewGuid(),
            HistoricalReviewResourceType.Fact,
            Guid.NewGuid(),
            2,
            HistoricalReviewEventType.Corrected,
            "admin-1",
            "Correction validée.",
            RecordedAtUtc);

        HistoricalReviewEvent restored = reviewEvent.ToDocument().ToDomain();

        Assert.Equal(reviewEvent.Id, restored.Id);
        Assert.Equal(reviewEvent.ResourceId, restored.ResourceId);
        Assert.Equal(reviewEvent.ResourceRevision, restored.ResourceRevision);
        Assert.Equal(reviewEvent.PrivateNote, restored.PrivateNote);
        Assert.Equal(0, restored.OccurredAtUtc.Ticks % TimeSpan.TicksPerMillisecond);
    }

    private static HistoricalReviewEvent CreateReviewEvent(
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int resourceRevision,
        HistoricalReviewEventType eventType)
    {
        return new HistoricalReviewEvent(
            Guid.NewGuid(),
            resourceType,
            resourceId,
            resourceRevision,
            eventType,
            "admin-1",
            null,
            RecordedAtUtc.AddMinutes(1));
    }
}
