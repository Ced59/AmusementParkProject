using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Tests.Features.History.Services;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class HistoryOwnerWriteCompatibilityTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("  Replacement owner  ", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public async Task Update_ShouldPreserveOmittedOwnerButRejectExplicitEmptyOwner(string? newOwnerName, bool expectedSuccess)
    {
        HistoryEvent existing = new HistoryEvent
        {
            Id = "history-1", Key = "acquisition", EntityType = HistoryEntityType.Park,
            OwnerId = "park-1", ParkId = "park-1", Year = 2026,
            EventType = "Acquisition", PreviousOwnerName = "Founding family", NewOwnerName = "Purchasing group",
        };
        Mock<IHistoryEventRepository> repository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync("history-1", true, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        repository.Setup(value => value.UpdateAsync("history-1", It.IsAny<HistoryEvent>(), It.IsAny<DateTime>(),
                It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, HistoryEvent value, DateTime _, Guid? _, Guid _, CancellationToken _) => value);
        repository.Setup(value => value.SetCanonicalizationAsync("history-1", It.IsAny<DateTime>(), It.IsAny<Guid?>(),
                HistoricalNarrativeCanonicalizationState.Canonicalized, HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>())).ReturnsAsync(new Park { Id = "park-1" });
        Mock<IHistoricalNarrativeCanonicalizer> canonicalizer = new Mock<IHistoricalNarrativeCanonicalizer>(MockBehavior.Strict);
        canonicalizer.Setup(value => value.CanonicalizeAsync(It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoricalNarrativeCanonicalizationResult(Guid.NewGuid(), HistoricalNarrativeCanonicalizationState.Canonicalized, Array.Empty<string>()));
        Mock<ISeoSitemapRefreshScheduler> scheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        scheduler.Setup(value => value.RequestRefreshAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        UpsertHistoryEventCommandHandler handler = new UpsertHistoryEventCommandHandler(
            repository.Object, parks.Object, Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            HistoricalCanonicalResourceRetractionServiceTestFactory.Create(Mock.Of<IHistoricalFactRepository>(MockBehavior.Strict)),
            canonicalizer.Object, scheduler.Object);

        ApplicationResult<HistoryEvent> result = await handler.HandleAsync(new UpsertHistoryEventCommand(new HistoryEventWriteModel
        {
            Id = "history-1", Key = "acquisition", EntityType = HistoryEntityType.Park,
            OwnerId = "park-1", Year = 2026, EventType = "Acquisition", NewOwnerName = newOwnerName,
            Sources = new[] { new HistorySourceReference { Url = "https://example.com/acquisition" } },
        }));

        Assert.Equal(expectedSuccess, result.IsSuccess);
        if (expectedSuccess)
        {
            Assert.Equal(newOwnerName?.Trim() ?? "Purchasing group", result.Value!.NewOwnerName);
            Assert.Equal("Founding family", result.Value.PreviousOwnerName);
        }
        else
        {
            Assert.Contains(result.Errors, static error => error.Code == "history.canonical-shape.invalid");
            Assert.Equal("Purchasing group", existing.NewOwnerName);
            repository.Verify(value => value.UpdateAsync(It.IsAny<string>(), It.IsAny<HistoryEvent>(), It.IsAny<DateTime>(),
                It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
