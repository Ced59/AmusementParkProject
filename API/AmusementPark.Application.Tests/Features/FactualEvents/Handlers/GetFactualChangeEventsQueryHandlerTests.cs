using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Handlers;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.FactualEvents.Queries;
using AmusementPark.Application.Features.FactualEvents.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.FactualEvents.Handlers;

public sealed class GetFactualChangeEventsQueryHandlerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 15, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_ShouldResolveReadableTargetAndParentNames()
    {
        FactualChangeEvent factualEvent = CreateParkItemEvent();
        Mock<IFactualChangeEventRepository> repository =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        repository.Setup(value => value.SearchAsync(
                It.IsAny<FactualChangeEventSearchCriteria>(),
                CancellationToken.None))
            .ReturnsAsync(new PagedResult<FactualChangeEvent>(new[] { factualEvent }, 1, 20, 1));
        Mock<IParkNameReadRepository> parkNames =
            new Mock<IParkNameReadRepository>(MockBehavior.Strict);
        parkNames.Setup(value => value.GetNamesByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string?> { ["park-1"] = "Parc exemple" });
        Mock<IParkItemNameReadRepository> parkItemNames =
            new Mock<IParkItemNameReadRepository>(MockBehavior.Strict);
        parkItemNames.Setup(value => value.GetNamesByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "ride-1" })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string?> { ["ride-1"] = "La Fusée" });
        GetFactualChangeEventsQueryHandler handler = new GetFactualChangeEventsQueryHandler(
            repository.Object,
            parkNames.Object,
            parkItemNames.Object);

        ApplicationResult<PagedResult<FactualChangeEventAdminResult>> result =
            await handler.HandleAsync(
                new GetFactualChangeEventsQuery(
                    new FactualChangeEventSearchCriteria(new PagedQuery())),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        FactualChangeEventAdminResult item = Assert.Single(result.Value!.Items);
        Assert.Equal("La Fusée", item.Target.TargetName);
        Assert.Equal("Parc exemple", item.Target.ParentParkName);
        Assert.False(item.CanBeDistributed);
        repository.VerifyAll();
        parkNames.VerifyAll();
        parkItemNames.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WithOversizedPage_ShouldRejectBeforeReading()
    {
        Mock<IFactualChangeEventRepository> repository =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        Mock<IParkNameReadRepository> parkNames =
            new Mock<IParkNameReadRepository>(MockBehavior.Strict);
        Mock<IParkItemNameReadRepository> parkItemNames =
            new Mock<IParkItemNameReadRepository>(MockBehavior.Strict);
        GetFactualChangeEventsQueryHandler handler = new GetFactualChangeEventsQueryHandler(
            repository.Object,
            parkNames.Object,
            parkItemNames.Object);

        ApplicationResult<PagedResult<FactualChangeEventAdminResult>> result =
            await handler.HandleAsync(
                new GetFactualChangeEventsQuery(
                    new FactualChangeEventSearchCriteria(new PagedQuery(1, 101))),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("factual-event.admin.search.invalid", Assert.Single(result.Errors).Code);
        repository.VerifyNoOtherCalls();
        parkNames.VerifyNoOtherCalls();
        parkItemNames.VerifyNoOtherCalls();
    }

    private static FactualChangeEvent CreateParkItemEvent()
    {
        return FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse("event-item-1"),
            FactualEventType.OpeningDateChanged,
            ChangeTarget.ForParkItem("ride-1", "park-1"),
            FactValue.FromDate(new DateOnly(2026, 6, 1)),
            FactValue.FromDate(new DateOnly(2026, 6, 2)),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Annonce officielle",
                "https://example.com/news",
                NowUtc.AddHours(-2)),
            DataConfidence.High,
            NowUtc.AddHours(-1),
            "park-item:ride-1:opening-date",
            1,
            NowUtc);
    }
}
