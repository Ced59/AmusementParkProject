using System.Text.Json;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class PublicHistoricalLineageHttpMappersTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ToHttp_ShouldUsePresentationKeysWithoutExposingInternalSubjectIdentifiers()
    {
        HistoricalSubject source = CreateSubject("internal-item-1", "Ancienne attraction");
        HistoricalSubject target = CreateSubject("internal-item-2", "Nouvelle attraction");
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(2001));
        Guid sourceId = Guid.NewGuid();
        HistoricalRelation relation = new HistoricalRelation(
            Guid.NewGuid(),
            source,
            target,
            HistoricalRelationType.ReplacedBy,
            HistoricalRelationDirection.Directed,
            period,
            HistoricalFactState.Verified,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            new[]
            {
                new HistoricalRelationSourceRevisionReference(
                    sourceId,
                    2,
                    new HistoricalSubjectKey(source.Type, source.Id, source.ContextParkId),
                    new HistoricalSubjectKey(target.Type, target.Id, target.ContextParkId),
                    HistoricalRelationType.ReplacedBy,
                    period,
                    HistoricalEvidencePosition.Supports,
                    new[]
                    {
                        HistoricalSourceScope.RelationSourceIdentity,
                        HistoricalSourceScope.RelationTargetIdentity,
                        HistoricalSourceScope.RelationType,
                        HistoricalSourceScope.Period,
                    }),
            },
            null,
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            2,
            1,
            RecordedAtUtc);
        PublicHistoricalLineageResult result = new(
            source,
            new PublicHistoricalLineageContextParkResult("public-park-1", "Parc public"),
            new[] { source, target },
            new[] { new PublicHistoricalRelationResult(relation, Array.Empty<HistoricalSourceReference>()) },
            false,
            false,
            4);

        PublicHistoricalLineageDto dto = result.ToHttp();
        string json = JsonSerializer.Serialize(dto);

        Assert.Equal("subject-1", dto.Root.Key);
        Assert.Equal("Parc public", Assert.IsType<PublicHistoricalLineageContextParkDto>(
            dto.ContextPark).Name);
        PublicHistoricalLineageRelationDto relationDto = Assert.Single(dto.Relations);
        Assert.Equal("subject-1", relationDto.SourceKey);
        Assert.Equal("subject-2", relationDto.TargetKey);
        Assert.DoesNotContain("internal-item-1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-item-2", json, StringComparison.Ordinal);
        Assert.DoesNotContain(relation.Id.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(sourceId.ToString(), json, StringComparison.OrdinalIgnoreCase);
    }

    private static HistoricalSubject CreateSubject(string id, string label)
    {
        return new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            id,
            label,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "internal-park-id");
    }
}
