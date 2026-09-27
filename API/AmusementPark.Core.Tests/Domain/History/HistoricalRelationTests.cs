using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalRelationTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenPublishedVerifiedRelationHasExactEvidence_ShouldAcceptRelation()
    {
        HistoricalRelation relation = CreatePublishedRelation();

        Assert.True(relation.IsDecisionEligible);
        Assert.Equal(HistoricalRelationType.ReplacedBy, relation.Type);
        Assert.Single(relation.SourceReferences);
    }

    [Fact]
    public void Constructor_WhenRelationTargetsItself_ShouldRejectRelation()
    {
        HistoricalSubject subject = CreateSubject("item-1", "Attraction");

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateDraftRelation(
                subject,
                subject,
                HistoricalRelationType.ReplacedBy,
                HistoricalRelationDirection.Directed));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRelation, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenIdentifierIsReusedAcrossParks_ShouldKeepSubjectsDistinct()
    {
        HistoricalSubject source = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            "shared-item",
            "Attraction du premier parc",
            HistoricalSubjectPublicationPolicy.HistoricalOnly,
            "park-1");
        HistoricalSubject target = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            "shared-item",
            "Attraction du second parc",
            HistoricalSubjectPublicationPolicy.HistoricalOnly,
            "park-2");

        HistoricalRelation relation = CreateDraftRelation(
            source,
            target,
            HistoricalRelationType.MovedTo,
            HistoricalRelationDirection.Directed);

        Assert.NotEqual(
            new HistoricalSubjectKey(source.Type, source.Id, source.ContextParkId),
            new HistoricalSubjectKey(target.Type, target.Id, target.ContextParkId));
        Assert.Equal("park-2", relation.Target.ContextParkId);
    }

    [Fact]
    public void Constructor_WhenDirectionDoesNotMatchType_ShouldRejectRelation()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateDraftRelation(
                CreateSubject("item-1", "Première attraction"),
                CreateSubject("item-2", "Deuxième attraction"),
                HistoricalRelationType.ReplacedBy,
                HistoricalRelationDirection.Symmetric));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRelation, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenSymmetricRelationUsesReverseOrder_ShouldRejectRelation()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateDraftRelation(
                CreateSubject("item-2", "Deuxième attraction"),
                CreateSubject("item-1", "Première attraction"),
                HistoricalRelationType.SamePhysicalAssetAs,
                HistoricalRelationDirection.Symmetric));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRelation, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenPublishedProbableRelationMissesTranslations_ShouldRejectRelation()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreatePublishedRelation(
                HistoricalFactState.Probable,
                new[] { new HistoricalLocalizedText("fr", "Cette relation reste probable.") }));

        Assert.Equal(HistoricalPersistenceErrorCodes.MissingUncertaintyExplanation, exception.ErrorCode);
    }

    [Fact]
    public void CreateRetraction_ShouldAppendWithdrawnRevisionWithoutMutatingOriginal()
    {
        HistoricalRelation original = CreatePublishedRelation();

        HistoricalRelation retraction = original.CreateRetraction(RecordedAtUtc.AddMinutes(1));

        Assert.Equal(HistoricalFactState.Verified, original.State);
        Assert.Equal(HistoricalFactState.Retracted, retraction.State);
        Assert.Equal(HistoricalPublicationState.Withdrawn, retraction.PublicationState);
        Assert.Equal(original.Revision + 1, retraction.Revision);
        Assert.Equal(original.Revision, retraction.SupersedesRevision);
    }

    internal static HistoricalRelation CreatePublishedRelation(
        HistoricalFactState state = HistoricalFactState.Verified,
        IReadOnlyCollection<HistoricalLocalizedText>? explanations = null,
        string sourceId = "item-1",
        string targetId = "item-2",
        Guid? relationId = null,
        HistoricalRelationType type = HistoricalRelationType.ReplacedBy,
        HistoricalRelationDirection direction = HistoricalRelationDirection.Directed)
    {
        HistoricalSubject source = CreateSubject(sourceId, $"Attraction {sourceId}");
        HistoricalSubject target = CreateSubject(targetId, $"Attraction {targetId}");
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(2001));
        Guid evidenceId = Guid.NewGuid();
        return new HistoricalRelation(
            relationId ?? Guid.NewGuid(),
            source,
            target,
            type,
            direction,
            period,
            state,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            explanations ?? (state is HistoricalFactState.Probable or HistoricalFactState.Disputed
                ? HistoricalLocalizationPolicy.SupportedLanguageCodes
                    .Select(static language => new HistoricalLocalizedText(
                        language,
                        "The available evidence requires a cautious interpretation."))
                    .ToArray()
                : Array.Empty<HistoricalLocalizedText>()),
            new[] { CreateEvidenceReference(evidenceId, source, target, type, period) },
            "Relation validée par la rédaction.",
            state == HistoricalFactState.Verified ? RecordedAtUtc.AddMinutes(-2) : null,
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            2,
            1,
            RecordedAtUtc);
    }

    internal static HistoricalRelationSourceRevisionReference CreateEvidenceReference(
        Guid sourceId,
        HistoricalSubject source,
        HistoricalSubject target,
        HistoricalRelationType type,
        HistoricalPeriod period,
        HistoricalEvidencePosition position = HistoricalEvidencePosition.Supports,
        IReadOnlyCollection<HistoricalSourceScope>? scopes = null)
    {
        return new HistoricalRelationSourceRevisionReference(
            sourceId,
            2,
            new HistoricalSubjectKey(source.Type, source.Id, source.ContextParkId),
            new HistoricalSubjectKey(target.Type, target.Id, target.ContextParkId),
            type,
            period,
            position,
            scopes ?? new[]
            {
                HistoricalSourceScope.RelationSourceIdentity,
                HistoricalSourceScope.RelationTargetIdentity,
                HistoricalSourceScope.RelationType,
                HistoricalSourceScope.Period,
            });
    }

    internal static HistoricalSubject CreateSubject(string id, string label)
    {
        return new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            id,
            label,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
    }

    private static HistoricalRelation CreateDraftRelation(
        HistoricalSubject source,
        HistoricalSubject target,
        HistoricalRelationType type,
        HistoricalRelationDirection direction)
    {
        return new HistoricalRelation(
            Guid.NewGuid(),
            source,
            target,
            type,
            direction,
            HistoricalPeriod.Point(HistoricalDate.ForYear(2001)),
            HistoricalFactState.Unverified,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            Array.Empty<HistoricalLocalizedText>(),
            Array.Empty<HistoricalRelationSourceRevisionReference>(),
            null,
            null,
            null,
            null,
            1,
            null,
            RecordedAtUtc);
    }
}
