using System.Text.Json;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class PublicParkHistoryHttpMappersTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TimelineMapping_DoesNotExposeEvidenceIdsAdminNotesOrUnresolvedTechnicalValues()
    {
        Guid sourceId = Guid.NewGuid();
        Guid factId = Guid.NewGuid();
        HistoricalSubject subject = new(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(2000));
        string structuredValue = "{\"previousId\":\"operator-private-old\",\"nextId\":\"operator-private-new\"}";
        HistoricalSourceScope[] scopes =
        {
            HistoricalSourceScope.SubjectIdentity,
            HistoricalSourceScope.HistoricalLabel,
            HistoricalSourceScope.FactType,
            HistoricalSourceScope.Period,
            HistoricalSourceScope.StructuredValue,
        };
        HistoricalSourceRevisionReference sourceReference = new(
            sourceId,
            2,
            subject.Type,
            subject.Id,
            HistoricalFactType.OperatorChange,
            period,
            HistoricalEvidencePosition.Supports,
            scopes,
            subject.HistoricalLabel,
            structuredValue,
            null,
            null,
            null,
            null,
            HistoricalAttributeKind.Operator,
            AttributeBoundaryMeaning.FirstDayOfNewValue);
        HistoricalFact fact = new(
            factId,
            subject,
            HistoricalFactType.OperatorChange,
            period,
            HistoricalFactState.Verified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            null,
            HistoricalAttributeKind.Operator,
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            null,
            new[] { sourceReference },
            structuredValue,
            null,
            null,
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion,
            2,
            1,
            RecordedAtUtc);
        HistoricalSourceReference source = new(
            sourceId,
            2,
            HistoricalSourceType.OfficialWebsite,
            "Source publique",
            "Parc témoin",
            "https://example.com/history",
            null,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 9, 20),
            "fr",
            null,
            scopes,
            "admin-secret-note",
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            RecordedAtUtc);
        Park park = new()
        {
            Id = "park-1",
            Name = "Parc témoin",
            IsVisible = true,
        };
        HistoryEvent narrative = new()
        {
            Id = "event-public-1",
            Slug = "opening-story",
            Titles = new()
            {
                new AmusementPark.Core.Localization.LocalizedText
                {
                    LanguageCode = "fr",
                    Value = "Le récit de l’ouverture",
                },
            },
            Article = new HistoryArticle
            {
                IsPublished = true,
                Slug = "opening-article",
            },
        };
        PublicParkHistoricalTimelineResult result = new(
            park,
            new PagedResult<PublicHistoricalTimelineEntryResult>(
                new[]
                {
                    new PublicHistoricalTimelineEntryResult(
                        fact,
                        new[] { source },
                        narrative,
                        "Nom public actuel"),
                },
                1,
                25,
                1),
            new Dictionary<string, string>());

        PublicParkHistoricalTimelineDto dto = result.ToHttp();
        PublicHistoricalTimelineEntryDto entry = Assert.Single(dto.Events);
        string json = JsonSerializer.Serialize(dto);

        Assert.Null(entry.PreviousDisplayValue);
        Assert.Null(entry.NextDisplayValue);
        Assert.Equal("Nom public actuel", entry.CurrentSubjectName);
        Assert.Equal(narrative.Id, entry.Narrative?.EventId);
        Assert.Equal("opening-article", entry.Narrative?.Slug);
        Assert.Equal("Le récit de l’ouverture", Assert.Single(entry.Narrative!.Titles).Value);
        Assert.DoesNotContain(factId.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(sourceId.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("admin-secret-note", json, StringComparison.Ordinal);
        Assert.DoesNotContain("operator-private-old", json, StringComparison.Ordinal);
        Assert.DoesNotContain("operator-private-new", json, StringComparison.Ordinal);
        Assert.Contains("Source publique", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SnapshotMapping_DoesNotExposeAmbiguityFactIds()
    {
        Guid internalFactId = Guid.NewGuid();
        HistoricalSubject subject = new(
            HistoricalSubjectType.ParkItem,
            "item-retired",
            "Nom actuel",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalSubjectSnapshot subjectSnapshot = new(
            subject,
            HistoricalOperationalState.Unknown,
            HistoricalPresenceExtent.None,
            Array.Empty<HistoricalPresenceInterval>(),
            Array.Empty<HistoricalAttributeSnapshot>(),
            new[]
            {
                new HistoricalSnapshotReason(
                    HistoricalSnapshotReasonCode.NoEligibleLifecycleFact,
                    new[] { internalFactId }),
            },
            Array.Empty<Guid>());
        HistoricalCoverage coverage = new(
            1,
            0,
            0,
            1,
            new HistoricalFieldCoverage(0, 1),
            new HistoricalFieldCoverage(0, 0),
            null,
            HistoricalCoverageStatus.Partial);
        ParkHistoricalSnapshot snapshot = new(
            "park-1",
            HistoricalInstant.ForYear(1998),
            new[] { subjectSnapshot },
            coverage,
            new[]
            {
                new HistoricalAmbiguity(
                    subject,
                    HistoricalSnapshotReasonCode.NoEligibleLifecycleFact,
                    null,
                    new[] { internalFactId }),
            },
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion);
        Park park = new()
        {
            Id = "park-1",
            Name = "Parc témoin",
            IsVisible = true,
        };
        PublicParkHistoricalSnapshotResult result = new(
            park,
            snapshot,
            Array.Empty<HistoricalFact>(),
            new Dictionary<string, string>(),
            true);

        PublicParkHistoricalSnapshotDto dto = result.ToHttp();
        string json = JsonSerializer.Serialize(dto);

        PublicHistoricalAmbiguityDto ambiguity = Assert.Single(dto.Ambiguities);
        Assert.Equal("NoEligibleLifecycleFact", ambiguity.Code);
        Assert.Equal("CurrentFallback", ambiguity.NameOrigin);
        PublicHistoricalSubjectSnapshotDto mappedSubject = Assert.Single(dto.Subjects);
        Assert.Equal("Nom actuel", mappedSubject.DisplayName);
        Assert.Equal("CurrentFallback", mappedSubject.NameOrigin);
        Assert.DoesNotContain(internalFactId.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.True(dto.IsIndexableKeyYear);
    }

    [Fact]
    public void SnapshotMapping_UsesHistoricalZoneNameAtRequestedInstant()
    {
        HistoricalSubject zoneSubject = new(
            HistoricalSubjectType.ParkZone,
            "zone-1",
            "Nom actuel",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalSubject itemSubject = new(
            HistoricalSubjectType.ParkItem,
            "item-1",
            "Attraction témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalAttributeSnapshot historicalZoneName = new(
            HistoricalAttributeKind.Name,
            HistoricalAttributeValueState.Known,
            "Ancien quartier",
            new[] { "Ancien quartier" },
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
        HistoricalAttributeSnapshot itemZone = new(
            HistoricalAttributeKind.Zone,
            HistoricalAttributeValueState.Known,
            zoneSubject.Id,
            new[] { zoneSubject.Id },
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
        HistoricalSubjectSnapshot zoneSnapshot = new(
            zoneSubject,
            HistoricalOperationalState.Unknown,
            HistoricalPresenceExtent.None,
            Array.Empty<HistoricalPresenceInterval>(),
            new[] { historicalZoneName },
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
        HistoricalSubjectSnapshot itemSnapshot = new(
            itemSubject,
            HistoricalOperationalState.Unknown,
            HistoricalPresenceExtent.None,
            Array.Empty<HistoricalPresenceInterval>(),
            new[] { itemZone },
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
        HistoricalCoverage coverage = new(
            2,
            0,
            0,
            2,
            new HistoricalFieldCoverage(1, 2),
            new HistoricalFieldCoverage(1, 1),
            null,
            HistoricalCoverageStatus.Partial);
        ParkHistoricalSnapshot snapshot = new(
            "park-1",
            HistoricalInstant.ForYear(1980),
            new[] { zoneSnapshot, itemSnapshot },
            coverage,
            Array.Empty<HistoricalAmbiguity>(),
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion);
        PublicParkHistoricalSnapshotResult result = new(
            new Park { Id = "park-1", Name = "Parc témoin", IsVisible = true },
            snapshot,
            Array.Empty<HistoricalFact>(),
            new Dictionary<string, string>
            {
                [zoneSubject.Id] = "Nom actuel",
            },
            false);

        PublicParkHistoricalSnapshotDto dto = result.ToHttp();

        PublicHistoricalSubjectSnapshotDto mappedItem = Assert.Single(
            dto.Subjects,
            static subject => subject.SubjectType == nameof(HistoricalSubjectType.ParkItem));
        PublicHistoricalAttributeDto mappedZone = Assert.Single(
            mappedItem.Attributes,
            static attribute => attribute.Kind == nameof(HistoricalAttributeKind.Zone));
        Assert.Equal("Ancien quartier", mappedZone.DisplayValue);
    }

    [Fact]
    public void ComparisonMapping_UsesOpaqueKeysAndResolvedHistoricalZoneNames()
    {
        HistoricalSubject zone = new(
            HistoricalSubjectType.ParkZone,
            "zone-technical",
            "Zone actuelle",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalSubject item = new(
            HistoricalSubjectType.ParkItem,
            "item-technical",
            "Attraction actuelle",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        ParkHistoricalSnapshot from = CreateComparisonSnapshot(
            1998,
            zone,
            item,
            "Ancienne zone",
            "Ancien nom");
        ParkHistoricalSnapshot to = CreateComparisonSnapshot(
            2026,
            zone,
            item,
            "Nouvelle zone",
            "Nouveau nom");
        ParkHistoricalComparison comparison = new ParkHistoricalComparisonBuilder().Build(from, to);
        PublicParkHistoricalComparisonResult result = new(
            new Park { Id = "park-1", Name = "Parc témoin", IsVisible = true },
            comparison,
            Array.Empty<HistoricalFact>(),
            new Dictionary<string, string>
            {
                [zone.Id] = "Zone actuelle",
                ["zone-old-technical"] = "Ancienne zone",
                ["zone-new-technical"] = "Nouvelle zone",
            });

        PublicParkHistoricalComparisonDto dto = result.ToHttp();
        PublicHistoricalSubjectComparisonDto mappedItem = Assert.Single(
            dto.Subjects,
            static subject => subject.SubjectType == nameof(HistoricalSubjectType.ParkItem));
        string json = JsonSerializer.Serialize(dto);

        Assert.StartsWith("subject-", mappedItem.ComparisonKey, StringComparison.Ordinal);
        Assert.Equal("Ancien nom", mappedItem.PreviousName);
        Assert.Equal("Nouveau nom", mappedItem.NextName);
        Assert.Equal("Ancienne zone", mappedItem.PreviousZoneName);
        Assert.Equal("Nouvelle zone", mappedItem.NextZoneName);
        Assert.True(mappedItem.IsRenamed);
        Assert.True(mappedItem.IsMoved);
        Assert.DoesNotContain(item.Id, json, StringComparison.Ordinal);
        Assert.DoesNotContain(zone.Id, json, StringComparison.Ordinal);
        Assert.DoesNotContain("zone-old-technical", json, StringComparison.Ordinal);
        Assert.DoesNotContain("zone-new-technical", json, StringComparison.Ordinal);
    }

    private static ParkHistoricalSnapshot CreateComparisonSnapshot(
        int year,
        HistoricalSubject zone,
        HistoricalSubject item,
        string zoneName,
        string itemName)
    {
        HistoricalAttributeSnapshot zoneNameAttribute = CreateKnownAttribute(
            HistoricalAttributeKind.Name,
            zoneName);
        HistoricalSubjectSnapshot zoneSnapshot = new(
            zone,
            HistoricalOperationalState.KnownOpen,
            HistoricalPresenceExtent.EntireRequestedPeriod,
            new[] { new HistoricalPresenceInterval(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)) },
            new[] { zoneNameAttribute },
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
        HistoricalSubjectSnapshot itemSnapshot = new(
            item,
            HistoricalOperationalState.KnownOpen,
            HistoricalPresenceExtent.EntireRequestedPeriod,
            new[] { new HistoricalPresenceInterval(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)) },
            new[]
            {
                CreateKnownAttribute(HistoricalAttributeKind.Name, itemName),
                CreateKnownAttribute(
                    HistoricalAttributeKind.Zone,
                    year < 2000 ? "zone-old-technical" : "zone-new-technical"),
                CreateKnownAttribute(HistoricalAttributeKind.Category, "Ride"),
            },
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
        HistoricalCoverage coverage = new(
            2,
            2,
            0,
            0,
            new HistoricalFieldCoverage(2, 2),
            new HistoricalFieldCoverage(1, 1),
            null,
            HistoricalCoverageStatus.HighConfidence);
        return new ParkHistoricalSnapshot(
            "park-1",
            HistoricalInstant.ForYear(year),
            new[] { zoneSnapshot, itemSnapshot },
            coverage,
            Array.Empty<HistoricalAmbiguity>(),
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion);
    }

    private static HistoricalAttributeSnapshot CreateKnownAttribute(
        HistoricalAttributeKind kind,
        string value)
    {
        return new HistoricalAttributeSnapshot(
            kind,
            HistoricalAttributeValueState.Known,
            value,
            new[] { value },
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
    }
}
