using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalRelationEvidenceValidatorTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Validate_WhenPublishedSourceCoversExactRelation_ShouldAcceptEvidence()
    {
        HistoricalRelation relation = HistoricalRelationTests.CreatePublishedRelation();
        HistoricalRelationSourceRevisionReference reference = Assert.Single(relation.SourceReferences);

        HistoricalRelationEvidenceValidator.Validate(
            relation,
            new[] { CreateSource(reference.SourceId, reference.Scopes) });
    }

    [Fact]
    public void Validate_WhenSourceRevisionDoesNotResolve_ShouldRejectEvidence()
    {
        HistoricalRelation relation = HistoricalRelationTests.CreatePublishedRelation();

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalRelationEvidenceValidator.Validate(
                    relation,
                    Array.Empty<HistoricalSourceReference>()));

        Assert.Equal(HistoricalPersistenceErrorCodes.MissingSource, exception.ErrorCode);
    }

    [Fact]
    public void HasAdmissiblePublicSupport_WhenOnlySourceIsWithdrawn_ShouldReturnFalse()
    {
        HistoricalRelation relation = HistoricalRelationTests.CreatePublishedRelation();
        HistoricalRelationSourceRevisionReference reference = Assert.Single(relation.SourceReferences);
        HistoricalSourceReference withdrawnSource = new HistoricalSourceReference(
            reference.SourceId,
            2,
            HistoricalSourceType.OfficialWebsite,
            "Historique officiel",
            "Parc exemple",
            "https://example.com/history",
            null,
            new DateOnly(2001, 1, 1),
            new DateOnly(2026, 9, 27),
            "fr",
            null,
            reference.Scopes,
            null,
            HistoricalSourceAccessibility.Withdrawn,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            RecordedAtUtc);

        bool hasPublicSupport = HistoricalRelationEvidenceValidator.HasAdmissiblePublicSupport(
            relation,
            new[] { withdrawnSource });

        Assert.False(hasPublicSupport);
    }

    [Fact]
    public void FilterCurrentlyAdmissiblePublicSources_WhenLatestRevisionIsWithdrawn_ShouldHidePublishedRevision()
    {
        HistoricalRelation relation = HistoricalRelationTests.CreatePublishedRelation();
        HistoricalRelationSourceRevisionReference reference = Assert.Single(relation.SourceReferences);
        HistoricalSourceReference publishedSource = CreateSource(reference.SourceId, reference.Scopes);
        HistoricalSourceReference withdrawnSource = new HistoricalSourceReference(
            reference.SourceId,
            3,
            HistoricalSourceType.OfficialWebsite,
            "Historique officiel",
            "Parc exemple",
            "https://example.com/history",
            null,
            new DateOnly(2001, 1, 1),
            new DateOnly(2026, 9, 27),
            "fr",
            null,
            reference.Scopes,
            null,
            HistoricalSourceAccessibility.Withdrawn,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            RecordedAtUtc.AddMinutes(1));

        IReadOnlyCollection<HistoricalSourceReference> publicSources =
            HistoricalRelationEvidenceValidator.FilterCurrentlyAdmissiblePublicSources(
                new[] { publishedSource },
                new[] { withdrawnSource });

        Assert.Empty(publicSources);
    }

    [Fact]
    public void Validate_WhenVerifiedRelationHasContradictingEvidence_ShouldRejectEvidence()
    {
        HistoricalRelation original = HistoricalRelationTests.CreatePublishedRelation();
        HistoricalRelationSourceRevisionReference originalReference = Assert.Single(original.SourceReferences);
        HistoricalRelationSourceRevisionReference contradictingReference =
            HistoricalRelationTests.CreateEvidenceReference(
                originalReference.SourceId,
                original.Source,
                original.Target,
                original.Type,
                original.Period,
                HistoricalEvidencePosition.Contradicts);
        HistoricalRelation relation = new HistoricalRelation(
            original.Id,
            original.Source,
            original.Target,
            original.Type,
            original.Direction,
            original.Period,
            original.State,
            original.WorkflowState,
            original.PublicationState,
            original.PublicUncertaintyExplanation,
            new[] { contradictingReference },
            original.EditorialNote,
            original.VerifiedAtUtc,
            original.PublishedAtUtc,
            original.PublicationMethodologyVersion,
            original.Revision,
            original.SupersedesRevision,
            original.RecordedAtUtc);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalRelationEvidenceValidator.Validate(
                    relation,
                    new[] { CreateSource(contradictingReference.SourceId, contradictingReference.Scopes) }));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRelation, exception.ErrorCode);
    }

    private static HistoricalSourceReference CreateSource(
        Guid id,
        IReadOnlyCollection<HistoricalSourceScope> scopes)
    {
        return new HistoricalSourceReference(
            id,
            2,
            HistoricalSourceType.OfficialWebsite,
            "Historique officiel",
            "Parc exemple",
            "https://example.com/history",
            null,
            new DateOnly(2001, 1, 1),
            new DateOnly(2026, 9, 27),
            "fr",
            null,
            scopes,
            null,
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            RecordedAtUtc);
    }
}
