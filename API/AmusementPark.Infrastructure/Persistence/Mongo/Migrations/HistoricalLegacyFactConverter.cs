using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

public sealed class HistoricalLegacyFactConverter
{
    private readonly IHistoricalFactRepository factRepository;
    private readonly HistoricalLegacySubjectResolver subjectResolver;
    private readonly HistoricalLegacySourceMigrator sourceMigrator;

    public HistoricalLegacyFactConverter(
        IHistoricalFactRepository factRepository,
        HistoricalLegacySubjectResolver subjectResolver,
        HistoricalLegacySourceMigrator sourceMigrator)
    {
        this.factRepository = factRepository ?? throw new ArgumentNullException(nameof(factRepository));
        this.subjectResolver = subjectResolver ?? throw new ArgumentNullException(nameof(subjectResolver));
        this.sourceMigrator = sourceMigrator ?? throw new ArgumentNullException(nameof(sourceMigrator));
    }

    public async Task<HistoricalLegacyMigrationResult> ConvertAndPersistAsync(
        HistoryEventDocument historyEvent,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        HistoricalLegacySubjectResolution subjectResolution = await this.subjectResolver.ResolveAsync(
            historyEvent,
            cancellationToken);
        if (subjectResolution.AnomalyCode is not null)
        {
            warnings.Add(subjectResolution.AnomalyCode);
        }

        if (!LegacyHistoryEventTypeMapper.TryMap(
                historyEvent.EntityType,
                historyEvent.EventType,
                out LegacyHistoryEventTypeMapping? mapping)
            || mapping is null)
        {
            warnings.Add(HistoricalLegacyMigrationAnomalyCodes.UnknownEventType);
            return Blocked(warnings);
        }

        HistoricalPeriod period;
        try
        {
            period = HistoricalPeriod.Point(BuildHistoricalDate(historyEvent));
        }
        catch (HistoricalTemporalValidationException)
        {
            warnings.Add(HistoricalLegacyMigrationAnomalyCodes.InvalidDate);
            return Blocked(warnings);
        }

        string? structuredValue = BuildStructuredValue(historyEvent, mapping);
        if (mapping.AttributeKind.HasValue && structuredValue is null)
        {
            warnings.Add(HistoricalLegacyMigrationAnomalyCodes.MissingStructuredValue);
            return Blocked(warnings);
        }

        DateTime recordedAtUtc = NormalizeUtc(
            historyEvent.UpdatedAt > historyEvent.CreatedAt
                ? historyEvent.UpdatedAt
                : historyEvent.CreatedAt);
        string historicalLabel = ResolveHistoricalLabel(historyEvent, subjectResolution.HistoricalLabel);
        HistoricalSubjectPublicationPolicy publicationPolicy = historyEvent.IsVisible
            ? subjectResolution.PublicationPolicy
            : HistoricalSubjectPublicationPolicy.Suppressed;
        HistoricalPublicationState publicationState = historyEvent.IsVisible
                && publicationPolicy != HistoricalSubjectPublicationPolicy.Suppressed
            ? HistoricalPublicationState.LegacyPublishedPendingReview
            : HistoricalPublicationState.Suppressed;
        HistoricalSubject subject = new HistoricalSubject(
            subjectResolution.SubjectType,
            subjectResolution.SubjectId,
            historicalLabel,
            publicationPolicy);
        string? otherTypeLabel = mapping.FactType == HistoricalFactType.Other
            ? historyEvent.EventType
            : null;
        HistoricalLegacySourceMigrationPlan[] sourcePlans = this.sourceMigrator.Prepare(
            historyEvent,
            subject,
            mapping,
            period,
            structuredValue,
            otherTypeLabel,
            recordedAtUtc,
            warnings);
        HistoricalSourceRevisionReference[] sourceReferences = sourcePlans
            .Select(static plan => plan.Reference)
            .ToArray();
        if (mapping.FactType == HistoricalFactType.Other && sourceReferences.Length == 0)
        {
            warnings.Add(HistoricalLegacyMigrationAnomalyCodes.IncompleteSource);
            return Blocked(warnings);
        }

        Guid factId = HistoricalLegacyMigrationIdentity.CreateGuid(
            HistoricalLegacyHistoryReplacementMigration.MigrationId,
            "fact",
            historyEvent.Id);
        IReadOnlyCollection<HistoricalLocalizedText> explanations =
            publicationState == HistoricalPublicationState.LegacyPublishedPendingReview
                ? BuildLegacyWarnings()
                : Array.Empty<HistoricalLocalizedText>();
        HistoricalFact fact = new HistoricalFact(
            factId,
            subject,
            mapping.FactType,
            period,
            HistoricalFactState.Unverified,
            historyEvent.IsMajor ? HistoricalImportance.Major : HistoricalImportance.Standard,
            HistoricalEditorialWorkflowState.EditorialReview,
            publicationState,
            explanations,
            mapping.LifecycleBoundaryMeaning,
            mapping.AttributeKind,
            mapping.AttributeBoundaryMeaning,
            null,
            sourceReferences,
            structuredValue,
            otherTypeLabel,
            historyEvent.Id,
            null,
            null,
            HistoricalLegacyHistoryReplacementMigration.MethodologyVersion,
            1,
            null,
            recordedAtUtc,
            HistoricalRevisionOrigin.LegacyMigration);
        HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
            HistoricalLegacyMigrationIdentity.CreateGuid(
                HistoricalLegacyHistoryReplacementMigration.MigrationId,
                "fact-review",
                historyEvent.Id),
            HistoricalReviewResourceType.Fact,
            factId,
            1,
            HistoricalReviewEventType.Migrated,
            HistoricalLegacyHistoryReplacementMigration.MigrationActor,
            BuildReviewNote(warnings),
            recordedAtUtc);
        HistoricalSubjectPublicationValidator.Validate(
            fact,
            publicationPolicy != HistoricalSubjectPublicationPolicy.Suppressed);
        HistoricalFactEvidenceValidator.Validate(
            fact,
            sourcePlans.Select(static plan => plan.Source).ToArray());
        HistoricalRevisionWriteDisposition outcome;
        try
        {
            await this.sourceMigrator.PersistAsync(sourcePlans, cancellationToken);
            outcome = await this.factRepository.AppendRevisionAsync(
                fact,
                reviewEvent,
                cancellationToken);
        }
        catch (HistoricalPersistenceValidationException exception)
        {
            throw new InvalidOperationException(
                "Canonical historical persistence diverged after successful migration prevalidation.",
                exception);
        }

        if (outcome == HistoricalRevisionWriteDisposition.Conflict)
        {
            throw new InvalidOperationException("A canonical historical fact migration identity collided.");
        }

        return new HistoricalLegacyMigrationResult(
            factId.ToString("N", CultureInfo.InvariantCulture),
            false,
            sourceReferences.Length,
            warnings);
    }

    internal static HistoricalDate BuildHistoricalDate(HistoryEventDocument historyEvent)
    {
        return historyEvent.DatePrecision switch
        {
            HistoryDatePrecision.Year => HistoricalDate.ForYear(historyEvent.Year),
            HistoryDatePrecision.Month => HistoricalDate.ForMonth(
                historyEvent.Year,
                historyEvent.Month ?? 0),
            HistoryDatePrecision.Day => HistoricalDate.ForDay(
                historyEvent.Year,
                historyEvent.Month ?? 0,
                historyEvent.Day ?? 0),
            _ => throw new HistoricalTemporalValidationException(
                HistoricalTemporalErrorCodes.InvalidPrecision,
                "The legacy historical date precision is invalid.",
                nameof(historyEvent.DatePrecision)),
        };
    }

    internal static string? BuildStructuredValue(
        HistoryEventDocument historyEvent,
        LegacyHistoryEventTypeMapping mapping)
    {
        if (!mapping.AttributeKind.HasValue)
        {
            return null;
        }

        Dictionary<string, string?> values = mapping.AttributeKind.Value switch
        {
            HistoricalAttributeKind.Name or HistoricalAttributeKind.MarketPositioning
                or HistoricalAttributeKind.Theme => new Dictionary<string, string?>
                {
                    ["previous"] = historyEvent.PreviousName,
                    ["next"] = historyEvent.NewName,
                },
            HistoricalAttributeKind.Logo => new Dictionary<string, string?>
                {
                    ["previousImageId"] = historyEvent.PreviousLogoImageId,
                    ["nextImageId"] = historyEvent.NewLogoImageId,
                },
            HistoricalAttributeKind.Operator => new Dictionary<string, string?>
                {
                    ["previousId"] = historyEvent.PreviousOperatorId,
                    ["nextId"] = historyEvent.NewOperatorId,
                },
            HistoricalAttributeKind.Owner => new Dictionary<string, string?>(),
            HistoricalAttributeKind.Location or HistoricalAttributeKind.Zone =>
                new Dictionary<string, string?> { ["label"] = historyEvent.LocationLabel },
            _ => new Dictionary<string, string?>(),
        };
        Dictionary<string, string> normalized = values
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value!.Trim(),
                StringComparer.Ordinal);
        return normalized.Count == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    internal static string ResolveHistoricalLabel(HistoryEventDocument historyEvent, string fallback)
    {
        string label = FirstNonEmpty(
                historyEvent.NewName,
                historyEvent.PreviousName,
                fallback)
            ?? fallback;
        return label.Length <= 300 ? label : label[..300];
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    internal static IReadOnlyCollection<HistoricalLocalizedText> BuildLegacyWarnings()
    {
        return new[]
        {
            new HistoricalLocalizedText("fr", "Cette information héritée reste à vérifier à partir de ses sources."),
            new HistoricalLocalizedText("en", "This legacy information still needs to be checked against its sources."),
            new HistoricalLocalizedText("de", "Diese übernommene Information muss noch anhand ihrer Quellen geprüft werden."),
            new HistoricalLocalizedText("nl", "Deze overgenomen informatie moet nog aan de hand van de bronnen worden gecontroleerd."),
            new HistoricalLocalizedText("it", "Questa informazione precedente deve ancora essere verificata con le sue fonti."),
            new HistoricalLocalizedText("es", "Esta información heredada todavía debe verificarse con sus fuentes."),
            new HistoricalLocalizedText("pl", "Ta odziedziczona informacja wymaga jeszcze sprawdzenia w źródłach."),
            new HistoricalLocalizedText("pt", "Esta informação herdada ainda precisa de ser verificada nas respetivas fontes."),
        };
    }

    private static string? BuildReviewNote(IReadOnlyCollection<string> warnings)
    {
        return warnings.Count == 0
            ? null
            : string.Concat("Migration HIST-04 : ", string.Join(", ", warnings));
    }

    private static HistoricalLegacyMigrationResult Blocked(IReadOnlyCollection<string> warnings)
    {
        return new HistoricalLegacyMigrationResult(null, true, 0, warnings);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }
}
