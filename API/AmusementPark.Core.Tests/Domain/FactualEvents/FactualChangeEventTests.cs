using AmusementPark.Core.Domain.FactualEvents;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.FactualEvents;

public sealed class FactualChangeEventTests
{
    private static readonly DateTime SourcePublishedAtUtc = At(8);
    private static readonly DateTime OccurredAtUtc = At(9);
    private static readonly DateTime CreatedAtUtc = At(10);

    [Fact]
    public void CreateDraft_ShouldCaptureAValidatedStructuredChange()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.High);

        Assert.Equal(FactualChangeStatus.Draft, factualEvent.Status);
        Assert.Equal(FactualEventCatalog.CurrentSchemaVersion, factualEvent.DefinitionVersion);
        Assert.Equal("park:park-1:name:2026-09-15", factualEvent.DeduplicationKey);
        Assert.Equal(1, factualEvent.Revision);
        Assert.Equal(1, factualEvent.Version);
        Assert.False(factualEvent.CanBeDistributed);
        Assert.Equal(CreatedAtUtc, factualEvent.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, factualEvent.UpdatedAtUtc);
    }

    [Fact]
    public void CreateDraft_WithIncompatibleTarget_ShouldRejectEvent()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => FactualChangeEvent.CreateDraft(
                FactualChangeEventId.Parse("event-1"),
                FactualEventType.OpeningDateConfirmed,
                ChangeTarget.ForPark("park-1"),
                FactValue.FromDate(new DateOnly(2026, 6, 1)),
                FactValue.FromDate(new DateOnly(2026, 6, 2)),
                CreateSource(),
                DataConfidence.High,
                OccurredAtUtc,
                "park:park-1:opening-date",
                1,
                CreatedAtUtc));

        Assert.Equal(FactualEventErrorCodes.IncompatibleTarget, exception.Code);
    }

    [Fact]
    public void CreateDraft_WithoutAnActualChange_ShouldRejectEvent()
    {
        FactValue value = FactValue.FromText("Example Park");

        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => FactualChangeEvent.CreateDraft(
                FactualChangeEventId.Parse("event-1"),
                FactualEventType.ParkNameChanged,
                ChangeTarget.ForPark("park-1"),
                value,
                value,
                CreateSource(),
                DataConfidence.High,
                OccurredAtUtc,
                "park:park-1:name:2026-09-15",
                1,
                CreatedAtUtc));

        Assert.Equal(FactualEventErrorCodes.UnchangedFact, exception.Code);
    }

    [Fact]
    public void CreateDraft_WithFutureSource_ShouldRejectChronology()
    {
        SourceReference futureSource = new SourceReference(
            SourceReferenceType.OfficialWebsite,
            "Example Park",
            "Announcement",
            "https://example.com/news",
            At(11));

        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => CreateDraft(DataConfidence.High, futureSource));

        Assert.Equal(FactualEventErrorCodes.InvalidTimestamp, exception.Code);
    }

    [Fact]
    public void ReplaceEvidence_ShouldOnlyMutateDraftWhenEvidenceChanges()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.Low);
        SourceReference replacement = new SourceReference(
            SourceReferenceType.OfficialDocument,
            "Example Park",
            "Official announcement",
            "https://example.com/official-news",
            SourcePublishedAtUtc);

        factualEvent.ReplaceEvidence(replacement, DataConfidence.High, At(11));
        factualEvent.ReplaceEvidence(replacement, DataConfidence.High, At(12));

        Assert.Equal(replacement, factualEvent.Source);
        Assert.Equal(DataConfidence.High, factualEvent.Confidence);
        Assert.Equal(2, factualEvent.Version);
        Assert.Equal(At(11), factualEvent.UpdatedAtUtc);
    }

    [Fact]
    public void Verify_WithLowConfidenceEvidence_ShouldRejectPromotion()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.Low);

        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => factualEvent.Verify(At(11)));

        Assert.Equal(FactualEventErrorCodes.InsufficientConfidence, exception.Code);
        Assert.Equal(FactualChangeStatus.Draft, factualEvent.Status);
        Assert.Equal(1, factualEvent.Version);
    }

    [Fact]
    public void VerifyThenPublish_ShouldMakeOnlyPublishedFactDistributable()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.Medium);

        factualEvent.Verify(At(11));

        Assert.Equal(FactualChangeStatus.Verified, factualEvent.Status);
        Assert.Equal(At(11), factualEvent.VerifiedAtUtc);
        Assert.False(factualEvent.CanBeDistributed);

        factualEvent.Publish(At(12));
        factualEvent.Publish(At(13));

        Assert.Equal(FactualChangeStatus.Published, factualEvent.Status);
        Assert.Equal(At(12), factualEvent.PublishedAtUtc);
        Assert.True(factualEvent.CanBeDistributed);
        Assert.Equal(3, factualEvent.Version);
        Assert.Equal(At(12), factualEvent.UpdatedAtUtc);
    }

    [Fact]
    public void Publish_FromDraft_ShouldRejectUnverifiedFact()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.High);

        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => factualEvent.Publish(At(11)));

        Assert.Equal(FactualEventErrorCodes.InvalidTransition, exception.Code);
        Assert.False(factualEvent.CanBeDistributed);
    }

    [Fact]
    public void Correct_ShouldLinkPublishedFactToItsSupersedingRevisionIdempotently()
    {
        FactualChangeEvent factualEvent = CreatePublished();
        FactualChangeEventId successorId = FactualChangeEventId.Parse("event-2");

        factualEvent.Correct(successorId, At(13));
        factualEvent.Correct(successorId, At(14));

        Assert.Equal(FactualChangeStatus.Corrected, factualEvent.Status);
        Assert.Equal(successorId, factualEvent.SupersededByEventId);
        Assert.Equal(At(13), factualEvent.TerminalAtUtc);
        Assert.False(factualEvent.CanBeDistributed);
        Assert.Equal(4, factualEvent.Version);
    }

    [Fact]
    public void Correct_WithOwnIdentifier_ShouldRejectCycle()
    {
        FactualChangeEvent factualEvent = CreatePublished();

        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => factualEvent.Correct(factualEvent.Id, At(13)));

        Assert.Equal(FactualEventErrorCodes.MissingSupersedingEvent, exception.Code);
    }

    [Fact]
    public void Retract_ShouldRequireReasonAndRemainIdempotent()
    {
        FactualChangeEvent factualEvent = CreatePublished();

        factualEvent.Retract("source-withdrawn", At(13));
        factualEvent.Retract(" source-withdrawn ", At(14));

        Assert.Equal(FactualChangeStatus.Retracted, factualEvent.Status);
        Assert.Equal("source-withdrawn", factualEvent.ReasonCode);
        Assert.Equal(At(13), factualEvent.TerminalAtUtc);
        Assert.False(factualEvent.CanBeDistributed);
        Assert.Equal(4, factualEvent.Version);
    }

    [Fact]
    public void Expire_ShouldCloseDraftWithoutInventingVerificationMetadata()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.Low);

        factualEvent.Expire(At(11));
        factualEvent.Expire(At(12));

        Assert.Equal(FactualChangeStatus.Expired, factualEvent.Status);
        Assert.Null(factualEvent.VerifiedAtUtc);
        Assert.Null(factualEvent.PublishedAtUtc);
        Assert.Equal(At(11), factualEvent.TerminalAtUtc);
        Assert.Equal(2, factualEvent.Version);
    }

    [Fact]
    public void Restore_WithPublishedStateMissingVerification_ShouldRejectCorruptState()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => Restore(
                FactualChangeStatus.Published,
                verifiedAtUtc: null,
                publishedAtUtc: At(12),
                terminalAtUtc: null,
                supersededByEventId: null,
                reasonCode: null));

        Assert.Equal(FactualEventErrorCodes.InvalidState, exception.Code);
    }

    [Fact]
    public void Restore_WithVerifiedLowConfidenceState_ShouldRejectImpossiblePromotion()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => Restore(
                FactualChangeStatus.Verified,
                verifiedAtUtc: At(11),
                publishedAtUtc: null,
                terminalAtUtc: null,
                supersededByEventId: null,
                reasonCode: null,
                confidence: DataConfidence.Low));

        Assert.Equal(FactualEventErrorCodes.InsufficientConfidence, exception.Code);
    }

    [Fact]
    public void Restore_WithLifecycleTimestampAfterUpdatedState_ShouldRejectCorruptState()
    {
        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => FactualChangeEvent.Restore(
                FactualChangeEventId.Parse("event-1"),
                FactualEventType.ParkNameChanged,
                FactualEventCatalog.CurrentSchemaVersion,
                ChangeTarget.ForPark("park-1"),
                FactValue.FromText("Old Park"),
                FactValue.FromText("Example Park"),
                CreateSource(),
                DataConfidence.High,
                OccurredAtUtc,
                "park:park-1:name:2026-09-15",
                1,
                FactualChangeStatus.Verified,
                CreatedAtUtc,
                CreatedAtUtc,
                At(11),
                null,
                null,
                null,
                null,
                1));

        Assert.Equal(FactualEventErrorCodes.InvalidTimestamp, exception.Code);
    }

    [Fact]
    public void Restore_WithSelfReferencingCorrection_ShouldRejectCycle()
    {
        FactualChangeEventId eventId = FactualChangeEventId.Parse("event-1");

        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => FactualChangeEvent.Restore(
                eventId,
                FactualEventType.ParkNameChanged,
                FactualEventCatalog.CurrentSchemaVersion,
                ChangeTarget.ForPark("park-1"),
                FactValue.FromText("Old Park"),
                FactValue.FromText("Example Park"),
                CreateSource(),
                DataConfidence.High,
                OccurredAtUtc,
                "park:park-1:name:2026-09-15",
                1,
                FactualChangeStatus.Corrected,
                CreatedAtUtc,
                At(13),
                At(11),
                At(12),
                At(13),
                eventId,
                null,
                4));

        Assert.Equal(FactualEventErrorCodes.MissingSupersedingEvent, exception.Code);
    }

    [Fact]
    public void HasSameLogicalRevisionAs_ShouldUseDeduplicationKeyAndRevision()
    {
        FactualChangeEvent first = CreateDraft(DataConfidence.High);
        FactualChangeEvent same = CreateDraft(DataConfidence.Medium);
        FactualChangeEvent nextRevision = FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse("event-3"),
            FactualEventType.ParkNameChanged,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Old Park"),
            FactValue.FromText("Example Park"),
            CreateSource(),
            DataConfidence.High,
            OccurredAtUtc,
            "park:park-1:name:2026-09-15",
            2,
            CreatedAtUtc);

        Assert.True(first.HasSameLogicalRevisionAs(same));
        Assert.False(first.HasSameLogicalRevisionAs(nextRevision));
    }

    [Fact]
    public void Mutation_WithOlderTimestamp_ShouldRejectLostUpdateChronology()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.High);
        factualEvent.Verify(At(11));

        FactualEventValidationException exception = Assert.Throws<FactualEventValidationException>(
            () => factualEvent.Publish(At(10)));

        Assert.Equal(FactualEventErrorCodes.InvalidTimestamp, exception.Code);
        Assert.Equal(FactualChangeStatus.Verified, factualEvent.Status);
    }

    private static FactualChangeEvent CreatePublished()
    {
        FactualChangeEvent factualEvent = CreateDraft(DataConfidence.High);
        factualEvent.Verify(At(11));
        factualEvent.Publish(At(12));
        return factualEvent;
    }

    private static FactualChangeEvent CreateDraft(
        DataConfidence confidence,
        SourceReference? source = null)
    {
        return FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse("event-1"),
            FactualEventType.ParkNameChanged,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Old Park"),
            FactValue.FromText("Example Park"),
            source ?? CreateSource(),
            confidence,
            OccurredAtUtc,
            "park:park-1:name:2026-09-15",
            1,
            CreatedAtUtc);
    }

    private static FactualChangeEvent Restore(
        FactualChangeStatus status,
        DateTime? verifiedAtUtc,
        DateTime? publishedAtUtc,
        DateTime? terminalAtUtc,
        FactualChangeEventId? supersededByEventId,
        string? reasonCode,
        DataConfidence confidence = DataConfidence.High)
    {
        return FactualChangeEvent.Restore(
            FactualChangeEventId.Parse("event-1"),
            FactualEventType.ParkNameChanged,
            FactualEventCatalog.CurrentSchemaVersion,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Old Park"),
            FactValue.FromText("Example Park"),
            CreateSource(),
            confidence,
            OccurredAtUtc,
            "park:park-1:name:2026-09-15",
            1,
            status,
            CreatedAtUtc,
            terminalAtUtc ?? publishedAtUtc ?? verifiedAtUtc ?? CreatedAtUtc,
            verifiedAtUtc,
            publishedAtUtc,
            terminalAtUtc,
            supersededByEventId,
            reasonCode,
            1);
    }

    private static SourceReference CreateSource()
    {
        return new SourceReference(
            SourceReferenceType.OfficialWebsite,
            "Example Park",
            "Announcement",
            "https://example.com/news",
            SourcePublishedAtUtc);
    }

    private static DateTime At(int hour)
    {
        return new DateTime(2026, 9, 15, hour, 0, 0, DateTimeKind.Utc);
    }
}
