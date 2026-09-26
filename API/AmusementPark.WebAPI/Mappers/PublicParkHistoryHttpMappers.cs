using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.History;

namespace AmusementPark.WebAPI.Mappers;

internal static class PublicParkHistoryHttpMappers
{
    public static PublicParkHistoricalTimelineDto ToHttp(this PublicParkHistoricalTimelineResult result)
    {
        return new PublicParkHistoricalTimelineDto
        {
            ParkId = result.Park.Id,
            ParkName = result.Park.Name ?? string.Empty,
            Events = result.Page.Items.Select(entry => entry.ToHttp(result.ZoneNames)).ToArray(),
            Pagination = new PaginationDto
            {
                CurrentPage = result.Page.Page,
                ItemsPerPage = result.Page.PageSize,
                TotalItems = checked((int)result.Page.TotalItems),
                TotalPages = result.Page.TotalPages,
            },
        };
    }

    public static PublicParkHistoricalSnapshotDto ToHttp(this PublicParkHistoricalSnapshotResult result)
    {
        Dictionary<Guid, HistoricalFact> factsById = result.Facts.ToDictionary(static fact => fact.Id);
        IReadOnlyDictionary<string, string> zoneNames = ResolveSnapshotZoneNames(
            result.Snapshot,
            result.ZoneNames);
        return new PublicParkHistoricalSnapshotDto
        {
            ParkId = result.Park.Id,
            ParkName = result.Park.Name ?? string.Empty,
            RequestedInstant = result.Snapshot.RequestedInstant.ToHttp(),
            Subjects = result.Snapshot.Subjects
                .Select(subject => subject.ToHttp(factsById, zoneNames))
                .ToArray(),
            Coverage = result.Snapshot.Coverage.ToHttp(),
            Ambiguities = result.Snapshot.Ambiguities
                .Select(static ambiguity => ambiguity.ToHttp())
                .ToArray(),
            MethodologyVersion = result.Snapshot.MethodologyVersion,
        };
    }

    private static IReadOnlyDictionary<string, string> ResolveSnapshotZoneNames(
        ParkHistoricalSnapshot snapshot,
        IReadOnlyDictionary<string, string> currentZoneNames)
    {
        Dictionary<string, string> zoneNames = new(currentZoneNames, StringComparer.Ordinal);
        foreach (HistoricalSubjectSnapshot zone in snapshot.Subjects.Where(
                     static subject => subject.Subject.Type == HistoricalSubjectType.ParkZone))
        {
            HistoricalAttributeSnapshot? historicalName = zone.Attributes.FirstOrDefault(
                static attribute => attribute.Kind == HistoricalAttributeKind.Name
                    && attribute.State == HistoricalAttributeValueState.Known
                    && !string.IsNullOrWhiteSpace(attribute.Value));
            if (historicalName is not null)
            {
                zoneNames[zone.Subject.Id] = historicalName.Value!;
            }
        }

        return zoneNames;
    }

    private static PublicHistoricalTimelineEntryDto ToHttp(
        this PublicHistoricalTimelineEntryResult result,
        IReadOnlyDictionary<string, string> zoneNames)
    {
        (string? PreviousValue, string? NextValue) values = ResolveTransitionValues(
            result.Fact,
            zoneNames);
        return new PublicHistoricalTimelineEntryDto
        {
            SubjectType = result.Fact.Subject.Type.ToString(),
            SubjectId = result.Fact.Subject.Id,
            SubjectLabel = ResolveSubjectLabel(result.Fact.Subject),
            FactType = result.Fact.Type.ToString(),
            Period = result.Fact.Period.ToHttp(),
            EvidenceState = result.Fact.State.ToString(),
            Importance = result.Fact.Importance.ToString(),
            AttributeKind = result.Fact.AttributeKind?.ToString(),
            PreviousDisplayValue = values.PreviousValue,
            NextDisplayValue = values.NextValue,
            OtherTypeLabel = result.Fact.OtherTypeLabel,
            UncertaintyExplanations = result.Fact.PublicUncertaintyExplanation
                .Select(static text => new LocalizedTextDto
                {
                    LanguageCode = text.LanguageCode,
                    Value = text.Value,
                })
                .ToArray(),
            Sources = result.Sources.Select(static source => source.ToHttp()).ToArray(),
        };
    }

    private static PublicHistoricalSubjectSnapshotDto ToHttp(
        this HistoricalSubjectSnapshot subject,
        IReadOnlyDictionary<Guid, HistoricalFact> factsById,
        IReadOnlyDictionary<string, string> zoneNames)
    {
        PublicHistoricalAttributeDto[] attributes = subject.Attributes
            .Select(attribute => attribute.ToHttp(zoneNames))
            .ToArray();
        PublicHistoricalAttributeDto? historicalName = attributes.FirstOrDefault(
            static attribute => attribute.Kind == nameof(HistoricalAttributeKind.Name)
                && attribute.State == nameof(HistoricalAttributeValueState.Known)
                && attribute.IsDisplayResolved
                && !string.IsNullOrWhiteSpace(attribute.DisplayValue));
        int sourceCount = subject.SupportingFactIds
            .Select(factId => factsById.GetValueOrDefault(factId))
            .Where(static fact => fact is not null)
            .SelectMany(static fact => fact!.SourceReferences)
            .Distinct()
            .Count();

        return new PublicHistoricalSubjectSnapshotDto
        {
            SubjectType = subject.Subject.Type.ToString(),
            SubjectId = subject.Subject.Id,
            DisplayName = historicalName?.DisplayValue ?? subject.Subject.HistoricalLabel,
            NameOrigin = ResolveNameOrigin(subject.Subject, historicalName),
            OperationalState = subject.OperationalState.ToString(),
            PresenceExtent = subject.PresenceExtent.ToString(),
            Attributes = attributes,
            ReasonCodes = subject.Reasons.Select(static reason => reason.Code.ToString()).ToArray(),
            SupportingSourceCount = sourceCount,
        };
    }

    private static string ResolveNameOrigin(
        HistoricalSubject subject,
        PublicHistoricalAttributeDto? historicalName)
    {
        if (historicalName is not null)
        {
            return "Historical";
        }

        return subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly
            ? "HistoricalLabel"
            : "CurrentFallback";
    }

    private static PublicHistoricalAttributeDto ToHttp(
        this HistoricalAttributeSnapshot attribute,
        IReadOnlyDictionary<string, string> zoneNames)
    {
        string? displayValue = ResolveDisplayValue(attribute.Kind, attribute.Value, zoneNames);
        string[] displayCandidates = attribute.Candidates
            .Select(candidate => ResolveDisplayValue(attribute.Kind, candidate, zoneNames))
            .Where(static candidate => candidate is not null)
            .Select(static candidate => candidate!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        bool requiresDisplayValue = attribute.State == HistoricalAttributeValueState.Known;
        bool requiresCandidates = attribute.State == HistoricalAttributeValueState.Ambiguous;
        bool isResolved = (!requiresDisplayValue || displayValue is not null)
            && (!requiresCandidates || displayCandidates.Length == attribute.Candidates.Count);

        return new PublicHistoricalAttributeDto
        {
            Kind = attribute.Kind.ToString(),
            State = attribute.State.ToString(),
            DisplayValue = displayValue,
            DisplayCandidates = displayCandidates,
            IsDisplayResolved = isResolved,
        };
    }

    private static PublicHistoricalCoverageDto ToHttp(this HistoricalCoverage coverage)
    {
        return new PublicHistoricalCoverageDto
        {
            TotalSubjectCount = coverage.TotalSubjectCount,
            ReliablePeriodSubjectCount = coverage.ReliablePeriodSubjectCount,
            PartialPeriodSubjectCount = coverage.PartialPeriodSubjectCount,
            UndatedSubjectCount = coverage.UndatedSubjectCount,
            Name = coverage.NameCoverage.ToHttp(),
            Zone = coverage.ZoneCoverage.ToHttp(),
            LastReviewedAtUtc = coverage.LastReviewedAtUtc,
            Status = coverage.Status.ToString(),
        };
    }

    private static PublicHistoricalFieldCoverageDto ToHttp(this HistoricalFieldCoverage coverage)
    {
        return new PublicHistoricalFieldCoverageDto
        {
            DocumentedSubjectCount = coverage.DocumentedSubjectCount,
            ApplicableSubjectCount = coverage.ApplicableSubjectCount,
            Percentage = coverage.Percentage,
            IsComplete = coverage.IsComplete,
        };
    }

    private static PublicHistoricalAmbiguityDto ToHttp(this HistoricalAmbiguity ambiguity)
    {
        return new PublicHistoricalAmbiguityDto
        {
            SubjectType = ambiguity.Subject.Type.ToString(),
            SubjectId = ambiguity.Subject.Id,
            SubjectLabel = ambiguity.Subject.HistoricalLabel,
            Code = ambiguity.Code.ToString(),
            AttributeKind = ambiguity.AttributeKind?.ToString(),
        };
    }

    private static PublicHistoricalPeriodDto ToHttp(this HistoricalPeriod period)
    {
        return new PublicHistoricalPeriodDto
        {
            Start = period.Start?.ToHttp(),
            End = period.End?.ToHttp(),
            StartConfidence = period.StartConfidence.ToString(),
            EndConfidence = period.EndConfidence.ToString(),
        };
    }

    private static PublicHistoricalDateDto ToHttp(this HistoricalDate date)
    {
        return new PublicHistoricalDateDto
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day,
            Precision = date.Precision.ToString(),
            IsApproximate = date.IsApproximate,
            Qualifier = date.Qualifier?.ToString(),
        };
    }

    private static PublicHistoricalDateDto ToHttp(this HistoricalInstant instant)
    {
        return new PublicHistoricalDateDto
        {
            Year = instant.Year,
            Month = instant.Month,
            Day = instant.Day,
            Precision = instant.Precision.ToString(),
        };
    }

    private static PublicHistoricalSourceDto ToHttp(this HistoricalSourceReference source)
    {
        return new PublicHistoricalSourceDto
        {
            Type = source.Type.ToString(),
            Title = source.Title,
            PublisherOrAuthor = source.PublisherOrAuthor,
            Url = source.Url,
            BibliographicReference = source.BibliographicReference,
            PublishedOn = source.PublishedOn,
            AccessedOn = source.AccessedOn,
            LanguageCode = source.LanguageCode,
            ArchiveUrl = source.ArchiveUrl,
            Accessibility = source.Accessibility.ToString(),
        };
    }

    private static (string? PreviousValue, string? NextValue) ResolveTransitionValues(
        HistoricalFact fact,
        IReadOnlyDictionary<string, string> zoneNames)
    {
        if (!HistoricalAttributeTransitionParser.TryParse(
                fact,
                out string? previousValue,
                out string? nextValue)
            || !fact.AttributeKind.HasValue)
        {
            return (null, null);
        }

        return (
            ResolveDisplayValue(fact.AttributeKind.Value, previousValue, zoneNames),
            ResolveDisplayValue(fact.AttributeKind.Value, nextValue, zoneNames));
    }

    private static string? ResolveDisplayValue(
        HistoricalAttributeKind kind,
        string? value,
        IReadOnlyDictionary<string, string> zoneNames)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = value.Trim();
        return kind switch
        {
            HistoricalAttributeKind.Name
                or HistoricalAttributeKind.Category
                or HistoricalAttributeKind.Theme
                or HistoricalAttributeKind.Location
                or HistoricalAttributeKind.Status
                or HistoricalAttributeKind.MarketPositioning => normalizedValue,
            HistoricalAttributeKind.Zone => zoneNames.GetValueOrDefault(normalizedValue),
            _ => null,
        };
    }

    private static string ResolveSubjectLabel(HistoricalSubject subject)
    {
        if (!string.Equals(subject.HistoricalLabel, subject.Id, StringComparison.OrdinalIgnoreCase))
        {
            return subject.HistoricalLabel;
        }

        return subject.Type switch
        {
            HistoricalSubjectType.Park => "Park",
            HistoricalSubjectType.ParkItem => "Park item",
            HistoricalSubjectType.StandaloneAttraction => "Standalone attraction",
            HistoricalSubjectType.ParkZone => "Park zone",
            HistoricalSubjectType.ParkOperator => "Park operator",
            HistoricalSubjectType.AttractionManufacturer => "Attraction manufacturer",
            _ => "Historical subject",
        };
    }
}
