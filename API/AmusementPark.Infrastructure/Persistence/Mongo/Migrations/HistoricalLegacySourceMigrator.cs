using System.Globalization;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

public sealed class HistoricalLegacySourceMigrator
{
    private readonly IHistoricalSourceRepository sourceRepository;

    public HistoricalLegacySourceMigrator(IHistoricalSourceRepository sourceRepository)
    {
        this.sourceRepository = sourceRepository
            ?? throw new ArgumentNullException(nameof(sourceRepository));
    }

    internal HistoricalLegacySourceMigrationPlan[] Prepare(
        HistoryEventDocument historyEvent,
        HistoricalSubject subject,
        LegacyHistoryEventTypeMapping mapping,
        HistoricalPeriod period,
        string? structuredValue,
        string? otherTypeLabel,
        DateTime recordedAtUtc,
        List<string> warnings)
    {
        List<HistoricalLegacySourceMigrationPlan> plans =
            new List<HistoricalLegacySourceMigrationPlan>();
        for (int index = 0; index < historyEvent.Sources.Count; index++)
        {
            HistorySourceReferenceDocument sourceDocument = historyEvent.Sources[index];
            string normalizedUrl = sourceDocument.Url?.Trim() ?? string.Empty;
            if (normalizedUrl.Length > 2000
                || !Uri.TryCreate(normalizedUrl, UriKind.Absolute, out Uri? uri)
                || uri.Scheme is not ("http" or "https"))
            {
                warnings.Add(HistoricalLegacyMigrationAnomalyCodes.InvalidSource);
                continue;
            }

            DateOnly recordedOn = DateOnly.FromDateTime(recordedAtUtc);
            bool hasValidAccessDate = DateOnly.TryParse(
                    sourceDocument.AccessedAt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly parsedAccessedOn)
                && parsedAccessedOn <= recordedOn;
            bool incomplete = string.IsNullOrWhiteSpace(sourceDocument.Label)
                || !hasValidAccessDate;
            if (incomplete)
            {
                warnings.Add(HistoricalLegacyMigrationAnomalyCodes.IncompleteSource);
            }

            DateOnly accessedOn = hasValidAccessDate ? parsedAccessedOn : recordedOn;
            string sourceTitle = NormalizeTitle(sourceDocument.Label, uri.Host);
            HistoricalSourceScope[] scopes = BuildSourceScopes(structuredValue);
            Guid sourceId = HistoricalLegacyMigrationIdentity.CreateGuid(
                HistoricalLegacyHistoryReplacementMigration.MigrationId,
                "source",
                historyEvent.Id,
                index);
            HistoricalSourceReference source = new HistoricalSourceReference(
                sourceId,
                1,
                HistoricalSourceType.Other,
                sourceTitle,
                uri.Host,
                normalizedUrl,
                null,
                null,
                accessedOn,
                null,
                null,
                scopes,
                incomplete ? "Référence importée à compléter après migration." : null,
                HistoricalSourceAccessibility.Accessible,
                HistoricalEditorialWorkflowState.EditorialReview,
                HistoricalPublicationState.LegacyPublishedPendingReview,
                recordedAtUtc,
                HistoricalRevisionOrigin.LegacyMigration);
            HistoricalReviewEvent sourceReview = new HistoricalReviewEvent(
                HistoricalLegacyMigrationIdentity.CreateGuid(
                    HistoricalLegacyHistoryReplacementMigration.MigrationId,
                    "source-review",
                    historyEvent.Id,
                    index),
                HistoricalReviewResourceType.Source,
                sourceId,
                1,
                HistoricalReviewEventType.Migrated,
                HistoricalLegacyHistoryReplacementMigration.MigrationActor,
                incomplete ? "Référence héritée incomplète." : null,
                recordedAtUtc);
            HistoricalSourceRevisionReference reference = new HistoricalSourceRevisionReference(
                sourceId,
                1,
                subject.Type,
                subject.Id,
                mapping.FactType,
                period,
                HistoricalEvidencePosition.Supports,
                scopes,
                subject.HistoricalLabel,
                structuredValue,
                null,
                historyEvent.Id,
                otherTypeLabel,
                mapping.LifecycleBoundaryMeaning,
                mapping.AttributeKind,
                mapping.AttributeBoundaryMeaning);
            plans.Add(new HistoricalLegacySourceMigrationPlan(source, sourceReview, reference));
        }

        return plans.ToArray();
    }

    internal async Task PersistAsync(
        IReadOnlyCollection<HistoricalLegacySourceMigrationPlan> plans,
        CancellationToken cancellationToken)
    {
        foreach (HistoricalLegacySourceMigrationPlan plan in plans)
        {
            HistoricalRevisionWriteDisposition outcome = await this.sourceRepository.AppendRevisionAsync(
                plan.Source,
                plan.ReviewEvent,
                cancellationToken);
            if (outcome == HistoricalRevisionWriteDisposition.Conflict)
            {
                throw new InvalidOperationException(
                    "A canonical historical source migration identity collided.");
            }
        }
    }

    private static HistoricalSourceScope[] BuildSourceScopes(string? structuredValue)
    {
        List<HistoricalSourceScope> scopes = new List<HistoricalSourceScope>
        {
            HistoricalSourceScope.SubjectIdentity,
            HistoricalSourceScope.HistoricalLabel,
            HistoricalSourceScope.FactType,
            HistoricalSourceScope.Period,
            HistoricalSourceScope.Narrative,
        };
        if (structuredValue is not null)
        {
            scopes.Add(HistoricalSourceScope.StructuredValue);
        }

        return scopes.ToArray();
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }

    private static string NormalizeTitle(string? value, string fallback)
    {
        string normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : new string(value.Where(static character => !char.IsControl(character)).ToArray()).Trim();
        return Truncate(string.IsNullOrWhiteSpace(normalized) ? fallback : normalized, 500);
    }
}
