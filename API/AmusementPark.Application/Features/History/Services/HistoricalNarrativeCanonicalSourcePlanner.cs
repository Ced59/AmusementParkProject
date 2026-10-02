using System.Globalization;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

internal sealed class HistoricalNarrativeCanonicalSourcePlanner
{
    internal static bool HasValidSource(HistoryEvent historyEvent)
    {
        return historyEvent.Sources.Any(static source =>
            Uri.TryCreate(source.Url?.Trim(), UriKind.Absolute, out Uri? uri)
            && uri.Scheme is "http" or "https");
    }

    internal HistoricalSourcePlan[] BuildPlans(
        HistoryEvent historyEvent,
        HistoricalSubject subject,
        LegacyHistoryEventTypeMapping mapping,
        HistoricalPeriod period,
        string? structuredValue,
        string? otherTypeLabel,
        DateTime recordedAtUtc,
        bool publish,
        List<string> warnings)
    {
        List<HistoricalSourcePlan> plans = new List<HistoricalSourcePlan>();
        for (int index = 0; index < historyEvent.Sources.Count; index++)
        {
            HistorySourceReference source = historyEvent.Sources[index];
            string normalizedUrl = source.Url?.Trim() ?? string.Empty;
            if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out Uri? uri)
                || uri.Scheme is not ("http" or "https"))
            {
                warnings.Add("history-canonicalization.invalid-source");
                continue;
            }

            DateOnly recordedOn = DateOnly.FromDateTime(recordedAtUtc);
            bool hasValidAccessDate = DateOnly.TryParse(
                    source.AccessedAt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly parsedAccessedOn)
                && parsedAccessedOn <= recordedOn;
            if (!hasValidAccessDate)
            {
                warnings.Add("history-canonicalization.source-access-date-normalized");
            }

            Guid sourceId = HistoricalNarrativeCanonicalIdentity.CreateGuid(
                HistoricalNarrativeCanonicalizationPolicy.Version,
                "source",
                historyEvent.Id,
                historyEvent.UpdatedAtUtc,
                index);
            HistoricalSourceReference draft = new HistoricalSourceReference(
                sourceId,
                1,
                HistoricalSourceType.Other,
                NormalizeTitle(source.Label, uri.Host),
                uri.Host,
                normalizedUrl,
                null,
                null,
                hasValidAccessDate ? parsedAccessedOn : recordedOn,
                null,
                null,
                BuildScopes(structuredValue),
                null,
                HistoricalSourceAccessibility.Accessible,
                HistoricalEditorialWorkflowState.Draft,
                HistoricalPublicationState.Draft,
                recordedAtUtc,
                HistoricalRevisionOrigin.Ordinary);
            HistoricalSourceReference published = publish
                ? CreateRevision(
                    CreateRevision(
                        CreateRevision(
                            draft,
                            HistoricalEditorialWorkflowState.EditorialReview,
                            recordedAtUtc),
                        HistoricalEditorialWorkflowState.StructuredValidation,
                        recordedAtUtc),
                    HistoricalEditorialWorkflowState.Published,
                    recordedAtUtc)
                : draft;
            plans.Add(new HistoricalSourcePlan(
                draft,
                published,
                subject,
                mapping,
                period,
                structuredValue,
                otherTypeLabel,
                historyEvent.Id));
        }

        return plans.ToArray();
    }

    internal HistoricalSourceReference CreateRevision(
        HistoricalSourceReference previous,
        HistoricalEditorialWorkflowState stage,
        DateTime recordedAtUtc)
    {
        return new HistoricalSourceReference(
            previous.Id,
            previous.Revision + 1,
            previous.Type,
            previous.Title,
            previous.PublisherOrAuthor,
            previous.Url,
            previous.BibliographicReference,
            previous.PublishedOn,
            previous.AccessedOn,
            previous.LanguageCode,
            previous.ArchiveUrl,
            previous.Scopes,
            previous.AdminNote,
            previous.Accessibility,
            stage,
            stage == HistoricalEditorialWorkflowState.Published
                ? HistoricalPublicationState.Published
                : HistoricalPublicationState.Draft,
            recordedAtUtc,
            HistoricalRevisionOrigin.Ordinary);
    }

    private static HistoricalSourceScope[] BuildScopes(string? structuredValue)
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

    private static string NormalizeTitle(string? value, string fallback)
    {
        string normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : new string(value.Where(static character => !char.IsControl(character)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = fallback;
        }

        return normalized.Length <= 500 ? normalized : normalized[..500];
    }
}
