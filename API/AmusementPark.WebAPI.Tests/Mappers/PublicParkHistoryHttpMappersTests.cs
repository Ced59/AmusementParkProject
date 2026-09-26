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
        PublicParkHistoricalTimelineResult result = new(
            park,
            new PagedResult<PublicHistoricalTimelineEntryResult>(
                new[] { new PublicHistoricalTimelineEntryResult(fact, new[] { source }) },
                1,
                25,
                1),
            new Dictionary<string, string>());

        PublicParkHistoricalTimelineDto dto = result.ToHttp();
        PublicHistoricalTimelineEntryDto entry = Assert.Single(dto.Events);
        string json = JsonSerializer.Serialize(dto);

        Assert.Null(entry.PreviousDisplayValue);
        Assert.Null(entry.NextDisplayValue);
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
            "Attraction disparue",
            HistoricalSubjectPublicationPolicy.HistoricalOnly,
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
            new Dictionary<string, string>());

        PublicParkHistoricalSnapshotDto dto = result.ToHttp();
        string json = JsonSerializer.Serialize(dto);

        Assert.Equal("NoEligibleLifecycleFact", Assert.Single(dto.Ambiguities).Code);
        PublicHistoricalSubjectSnapshotDto mappedSubject = Assert.Single(dto.Subjects);
        Assert.Equal("Attraction disparue", mappedSubject.DisplayName);
        Assert.Equal("HistoricalLabel", mappedSubject.NameOrigin);
        Assert.DoesNotContain(internalFactId.ToString(), json, StringComparison.OrdinalIgnoreCase);
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
            });

        PublicParkHistoricalSnapshotDto dto = result.ToHttp();

        PublicHistoricalSubjectSnapshotDto mappedItem = Assert.Single(
            dto.Subjects,
            static subject => subject.SubjectType == nameof(HistoricalSubjectType.ParkItem));
        PublicHistoricalAttributeDto mappedZone = Assert.Single(
            mappedItem.Attributes,
            static attribute => attribute.Kind == nameof(HistoricalAttributeKind.Zone));
        Assert.Equal("Ancien quartier", mappedZone.DisplayValue);
    }
}
