using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Migrations;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Migrations;

public sealed class HistoricalLegacySourceMigratorTests
{
    [Fact]
    public void Prepare_WhenFactWouldExceedSourceLimit_ShouldNotPersistOrphans()
    {
        Mock<IHistoricalSourceRepository> sourceRepository =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        HistoricalLegacySourceMigrator migrator = new HistoricalLegacySourceMigrator(
            sourceRepository.Object);
        DateTime recordedAtUtc = new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);
        HistoryEventDocument historyEvent = new HistoryEventDocument
        {
            Id = "legacy-event",
            Sources = Enumerable.Range(0, 101)
                .Select(index => new HistorySourceReferenceDocument
                {
                    Label = string.Concat("Source ", index),
                    Url = string.Concat("https://example.com/source/", index),
                    AccessedAt = "2026-09-25",
                })
                .ToList(),
        };
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc exemple",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(1998));
        LegacyHistoryEventTypeMapping mapping = new LegacyHistoryEventTypeMapping(
            HistoricalFactType.Opening,
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null);
        List<string> warnings = new List<string>();

        HistoricalLegacySourceMigrationPlan[] plans = migrator.Prepare(
            historyEvent,
            subject,
            mapping,
            period,
            null,
            null,
            recordedAtUtc,
            warnings);

        Assert.Equal(101, plans.Length);
        Assert.Throws<HistoricalPersistenceValidationException>(() => new HistoricalFact(
            Guid.NewGuid(),
            subject,
            HistoricalFactType.Opening,
            period,
            HistoricalFactState.Unverified,
            HistoricalImportance.Standard,
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalLocalizationPolicy.SupportedLanguageCodes
                .Select(static code => new HistoricalLocalizedText(code, "À vérifier."))
                .ToArray(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            plans.Select(static plan => plan.Reference).ToArray(),
            null,
            null,
            historyEvent.Id,
            null,
            null,
            "hist-v1-legacy",
            1,
            null,
            recordedAtUtc,
            HistoricalRevisionOrigin.LegacyMigration));
        sourceRepository.VerifyNoOtherCalls();
    }
}
