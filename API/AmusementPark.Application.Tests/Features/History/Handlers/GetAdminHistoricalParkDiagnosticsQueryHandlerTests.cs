using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class GetAdminHistoricalParkDiagnosticsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithoutParkId_ShouldRejectBeforeReadingRepositories()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zones = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> facts = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalRelationRepository> relations = new Mock<IHistoricalRelationRepository>(MockBehavior.Strict);
        Mock<IHistoricalVisitDiagnosticsReader> visits =
            new Mock<IHistoricalVisitDiagnosticsReader>(MockBehavior.Strict);
        GetAdminHistoricalParkDiagnosticsQueryHandler handler = CreateHandler(
            parks,
            items,
            zones,
            facts,
            relations,
            visits);

        ApplicationResult<AdminHistoricalParkDiagnosticsResult> result =
            await handler.HandleAsync(new GetAdminHistoricalParkDiagnosticsQuery(" "));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "validation.required");
        parks.VerifyNoOtherCalls();
        items.VerifyNoOtherCalls();
        zones.VerifyNoOtherCalls();
        facts.VerifyNoOtherCalls();
        relations.VerifyNoOtherCalls();
        visits.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeHiddenParkSubjectsAndAnonymousVisitCounts()
    {
        Park park = PublicParkHistoryTestData.CreatePark(false);
        ParkItem item = PublicParkHistoryTestData.CreateParkItem("item-1", "Montagnes russes", false);
        ParkZone zone = PublicParkHistoryTestData.CreateParkZone("zone-1", "Village", false);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zones = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> facts = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalRelationRepository> relations = new Mock<IHistoricalRelationRepository>(MockBehavior.Strict);
        Mock<IHistoricalVisitDiagnosticsReader> visits =
            new Mock<IHistoricalVisitDiagnosticsReader>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                "park-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        items.Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        zones.Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { zone });
        facts.Setup(repository => repository.GetLatestRevisionsForParkAsync(
                "park-1",
                It.Is<IReadOnlyCollection<HistoricalSubject>>(subjects =>
                    subjects.Count == 3
                    && subjects.Any(subject => subject.Type == HistoricalSubjectType.ParkItem
                        && subject.Id == "item-1")
                    && subjects.Any(subject => subject.Type == HistoricalSubjectType.ParkZone
                        && subject.Id == "zone-1")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoricalFact>());
        relations.Setup(repository => repository.GetLatestRevisionsForParkAsync(
                "park-1",
                It.Is<IReadOnlyCollection<HistoricalSubjectKey>>(subjects =>
                    subjects.Count == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoricalRelation>());
        visits.Setup(reader => reader.GetCountsAsync(
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoricalVisitDiagnosticCounts(4, 1, 3));
        GetAdminHistoricalParkDiagnosticsQueryHandler handler = CreateHandler(
            parks,
            items,
            zones,
            facts,
            relations,
            visits);

        ApplicationResult<AdminHistoricalParkDiagnosticsResult> result =
            await handler.HandleAsync(new GetAdminHistoricalParkDiagnosticsQuery(" park-1 "));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Parc témoin", result.Value.ParkName);
        Assert.Equal(4, result.Value.VisitCounts.PotentiallyInconsistentVisitCount);
        Assert.Empty(result.Value.Diagnostics.Issues);
        Assert.False(result.Value.RolloutGate.IsOpen);
        parks.VerifyAll();
        items.VerifyAll();
        zones.VerifyAll();
        facts.VerifyAll();
        relations.VerifyAll();
        visits.VerifyAll();
    }

    private static GetAdminHistoricalParkDiagnosticsQueryHandler CreateHandler(
        Mock<IParkRepository> parks,
        Mock<IParkItemRepository> items,
        Mock<IParkZoneRepository> zones,
        Mock<IHistoricalFactRepository> facts,
        Mock<IHistoricalRelationRepository> relations,
        Mock<IHistoricalVisitDiagnosticsReader> visits)
    {
        return new GetAdminHistoricalParkDiagnosticsQueryHandler(
            new HistoricalParkEditorialScopeLoader(
                parks.Object,
                items.Object,
                zones.Object),
            facts.Object,
            relations.Object,
            visits.Object,
            new HistoricalParkDiagnosticsEvaluator(),
            new HistoricalParkRolloutGateAssessmentService(
                new ParkHistoricalSnapshotBuilder(),
                new HistoricalParkRolloutGateEvaluator()));
    }
}
