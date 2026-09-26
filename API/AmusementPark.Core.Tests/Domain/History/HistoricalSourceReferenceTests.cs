using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalSourceReferenceTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_ShouldKeepAuditablePublishedSource()
    {
        HistoricalSourceReference source = CreatePublishedSource();

        Assert.Equal("https://example.com/archive", source.Url);
        Assert.Equal(new DateOnly(2026, 9, 25), source.AccessedOn);
        Assert.Equal(HistoricalPublicationState.Published, source.PublicationState);
        Assert.Equal(new[] { HistoricalSourceScope.Period }, source.Scopes);
    }

    [Fact]
    public void Constructor_WithoutStableReference_ShouldRejectSource()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => new HistoricalSourceReference(
                Guid.NewGuid(),
                1,
                HistoricalSourceType.Archive,
                "Archive du parc",
                "Archives municipales",
                null,
                null,
                null,
                new DateOnly(2026, 9, 25),
                "fr",
                null,
                new[] { HistoricalSourceScope.Period },
                null,
                HistoricalSourceAccessibility.Accessible,
                HistoricalEditorialWorkflowState.Draft,
                HistoricalPublicationState.Draft,
                RecordedAtUtc));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidSourceReference, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenPublishedSourceIsWithdrawn_ShouldRejectSource()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreatePublishedSource(
                HistoricalSourceAccessibility.Withdrawn));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenPublishedWorkflowRemainsDraft_ShouldRejectSource()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => new HistoricalSourceReference(
                Guid.NewGuid(),
                1,
                HistoricalSourceType.OfficialWebsite,
                "Page officielle",
                "Parc exemple",
                "https://example.com/history",
                null,
                null,
                new DateOnly(2026, 9, 25),
                "fr",
                null,
                new[] { HistoricalSourceScope.Period },
                null,
                HistoricalSourceAccessibility.Accessible,
                HistoricalEditorialWorkflowState.Published,
                HistoricalPublicationState.Draft,
                RecordedAtUtc));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_ShouldExposeValidatedScopesAsReadOnly()
    {
        HistoricalSourceReference source = CreatePublishedSource();
        IList<HistoricalSourceScope> scopes = Assert.IsAssignableFrom<IList<HistoricalSourceScope>>(source.Scopes);

        Assert.Throws<NotSupportedException>(() => scopes[0] = HistoricalSourceScope.StructuredValue);
    }

    [Fact]
    public void Constructor_WhenFirstRevisionClaimsCorrection_ShouldRejectSource()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreatePublishedSource(
                workflowState: HistoricalEditorialWorkflowState.Corrected));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
    }

    private static HistoricalSourceReference CreatePublishedSource(
        HistoricalSourceAccessibility accessibility = HistoricalSourceAccessibility.Archived,
        HistoricalEditorialWorkflowState workflowState = HistoricalEditorialWorkflowState.Published)
    {
        return new HistoricalSourceReference(
            Guid.NewGuid(),
            1,
            HistoricalSourceType.Archive,
            "Archive du parc",
            "Archives municipales",
            "https://example.com/archive",
            null,
            new DateOnly(1998, 5, 12),
            new DateOnly(2026, 9, 25),
            "fr",
            "https://web.archive.org/example",
            new[] { HistoricalSourceScope.Period },
            "Preuve relue.",
            accessibility,
            workflowState,
            HistoricalPublicationState.Published,
            RecordedAtUtc);
    }
}
