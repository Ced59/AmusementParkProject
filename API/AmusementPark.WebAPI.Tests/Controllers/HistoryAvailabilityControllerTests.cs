using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class HistoryAvailabilityControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Availability_UsesTheSamePublicTimelineAndReturnsOnlyItsExistence(bool hasEvents)
    {
        Mock<IQueryHandler<GetParkItemHistoryTimelineQuery, ApplicationResult<HistoryTimelineResult>>> timeline = new();
        timeline.Setup(handler => handler.HandleAsync(It.Is<GetParkItemHistoryTimelineQuery>(query =>
                query.ParkItemId == "item" && !query.IncludeHidden), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<HistoryTimelineResult>.Success(new HistoryTimelineResult
            {
                Events = hasEvents ? new[] { new HistoryTimelineEventResult() } : Array.Empty<HistoryTimelineEventResult>(),
            }));
        HistoryController controller = new HistoryController(
            Mock.Of<IQueryHandler<GetParkHistoryTimelineQuery, ApplicationResult<HistoryTimelineResult>>>(),
            timeline.Object,
            Mock.Of<IQueryHandler<GetHistoryArticleQuery, ApplicationResult<HistoryArticleResult>>>(),
            Mock.Of<IQueryHandler<GetLatestHistoryArticlesQuery, ApplicationResult<IReadOnlyCollection<HistoryArticleResult>>>>(),
            Mock.Of<IQueryHandler<GetHistoryEventsPageQuery, ApplicationResult<PagedResult<HistoryTimelineEventResult>>>>(),
            Mock.Of<ICommandHandler<UpsertHistoryEventCommand, ApplicationResult<HistoryEvent>>>(),
            Mock.Of<ICommandHandler<DeleteHistoryEventCommand, ApplicationResult>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.HasParkItemHistoryAsync("item"));
        Assert.Equal(hasEvents, Assert.IsType<bool>(result.Value));
        timeline.VerifyAll();
    }
}
