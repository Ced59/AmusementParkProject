using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Contracts.LiveData;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class PublicLiveControllerTests
{
    [Fact]
    public async Task GetParkAsync_ShouldReturnAttributionAndEntityTag()
    {
        PublicLiveTargetResult target = CreateTarget();
        PublicLiveController controller = CreateController(target);

        IActionResult result = await controller.GetParkAsync("park-1", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        AmusementPark.WebAPI.Contracts.LiveData.PublicLiveTargetDto dto =
            Assert.IsType<AmusementPark.WebAPI.Contracts.LiveData.PublicLiveTargetDto>(ok.Value);
        Assert.Equal("Powered by ThemeParks.wiki", dto.Source?.AttributionText);
        Assert.Equal(0, Assert.Single(dto.Queues).WaitTimeMinutes);
        Assert.StartsWith("\"", controller.Response.Headers.ETag.ToString(), StringComparison.Ordinal);
        Assert.Equal("public,max-age=0,must-revalidate", controller.Response.Headers.CacheControl);
        DateTime transition = Assert.IsType<DateTime>(
            controller.HttpContext.Items[
                PublicLiveExpirationOutputCachePolicy.FreshnessTransitionItemKey]);
        Assert.Equal(target.FreshnessTransitionAtUtc, transition);
    }

    [Fact]
    public async Task GetParkAsync_WhenEntityTagMatches_ShouldReturnNotModified()
    {
        PublicLiveTargetResult target = CreateTarget();
        PublicLiveController controller = CreateController(target);
        string entityTag = PublicLiveEntityTagFactory.Create(target.ToHttp());
        controller.Request.Headers.IfNoneMatch = $"W/{entityTag}";

        IActionResult result = await controller.GetParkAsync("park-1", CancellationToken.None);

        StatusCodeResult status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, status.StatusCode);
    }

    [Fact]
    public void EntityTag_ShouldIgnoreResponseClockButChangeWithOperationalFacts()
    {
        PublicLiveTargetDto initial = CreateTarget().ToHttp();
        PublicLiveTargetDto aged = initial with
        {
            AsOfUtc = initial.AsOfUtc.AddMinutes(4),
            AgeSeconds = initial.AgeSeconds + 240,
        };
        PublicLiveTargetDto changed = aged with { Status = LiveOperationalStatus.Closed.ToString() };

        Assert.Equal(
            PublicLiveEntityTagFactory.Create(initial),
            PublicLiveEntityTagFactory.Create(aged));
        Assert.NotEqual(
            PublicLiveEntityTagFactory.Create(initial),
            PublicLiveEntityTagFactory.Create(changed));
    }

    [Fact]
    public async Task GetParkItemHistoryAsync_ShouldReturnAggregatesAndEntityTag()
    {
        PublicLiveHistoryResult history = CreateHistory();
        PublicLiveController controller = CreateController(CreateTarget(), history);

        IActionResult result = await controller.GetParkItemHistoryAsync(
            "item-1",
            null,
            null,
            "hour",
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PublicLiveHistoryDto dto = Assert.IsType<PublicLiveHistoryDto>(ok.Value);
        Assert.Equal("Attraction", dto.DisplayName);
        Assert.Equal("Insufficient", dto.DataStatus);
        Assert.Equal(15d, Assert.Single(dto.Hours).MedianMinutes);
        Assert.Equal("Powered by ThemeParks.wiki", dto.Source.AttributionText);
        Assert.StartsWith("\"", controller.Response.Headers.ETag.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetParkItemForecastAsync_ShouldReturnEvidenceWithoutTechnicalTargetIds()
    {
        PublicLiveForecastResult forecast = CreateForecast();
        PublicLiveController controller = CreateController(
            CreateTarget(),
            forecastResult: forecast);

        IActionResult result = await controller.GetParkItemForecastAsync(
            "item-1",
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PublicLiveForecastDto dto = Assert.IsType<PublicLiveForecastDto>(ok.Value);
        Assert.Equal("Attraction", dto.TargetDisplayName);
        Assert.Equal(25d, dto.ExpectedWaitMinutes);
        Assert.Equal(LiveWaitForecastBacktestPolicy.CandidateMethod, dto.Method);
        Assert.Equal(4d, dto.MeanAbsoluteErrorMinutes);
        Assert.DoesNotContain("item-1", System.Text.Json.JsonSerializer.Serialize(dto));
        Assert.Equal(
            TimeSpan.FromMinutes(15),
            controller.HttpContext.Items[
                PublicLiveExpirationOutputCachePolicy.MaximumLifetimeItemKey]);
    }

    private static PublicLiveController CreateController(
        PublicLiveTargetResult target,
        PublicLiveHistoryResult? historyResult = null,
        PublicLiveForecastResult? forecastResult = null)
    {
        Mock<IQueryHandler<GetPublicParkLiveQuery, ApplicationResult<PublicLiveTargetResult>>> park =
            new Mock<IQueryHandler<GetPublicParkLiveQuery, ApplicationResult<PublicLiveTargetResult>>>(
                MockBehavior.Strict);
        Mock<IQueryHandler<GetPublicParkItemLiveQuery, ApplicationResult<PublicLiveTargetResult>>> item =
            new Mock<IQueryHandler<GetPublicParkItemLiveQuery, ApplicationResult<PublicLiveTargetResult>>>(
                MockBehavior.Strict);
        Mock<IQueryHandler<GetPublicParkLiveItemsQuery, ApplicationResult<PublicParkLiveItemsResult>>> items =
            new Mock<IQueryHandler<GetPublicParkLiveItemsQuery, ApplicationResult<PublicParkLiveItemsResult>>>(
                MockBehavior.Strict);
        Mock<IQueryHandler<GetPublicParkItemLiveHistoryQuery, ApplicationResult<PublicLiveHistoryResult>>> history =
            new Mock<IQueryHandler<GetPublicParkItemLiveHistoryQuery, ApplicationResult<PublicLiveHistoryResult>>>(
                MockBehavior.Strict);
        Mock<IQueryHandler<GetPublicParkItemLiveForecastQuery, ApplicationResult<PublicLiveForecastResult>>> forecast =
            new Mock<IQueryHandler<GetPublicParkItemLiveForecastQuery, ApplicationResult<PublicLiveForecastResult>>>(
                MockBehavior.Strict);
        park.Setup(handler => handler.HandleAsync(
                It.Is<GetPublicParkLiveQuery>(query => query.ParkId == "park-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PublicLiveTargetResult>.Success(target));
        if (historyResult is not null)
        {
            history.Setup(handler => handler.HandleAsync(
                    It.Is<GetPublicParkItemLiveHistoryQuery>(query =>
                        query.ParkItemId == "item-1" && query.Bucket == "hour"),
                    CancellationToken.None))
                .ReturnsAsync(ApplicationResult<PublicLiveHistoryResult>.Success(historyResult));
        }
        if (forecastResult is not null)
        {
            forecast.Setup(handler => handler.HandleAsync(
                    It.Is<GetPublicParkItemLiveForecastQuery>(query => query.ParkItemId == "item-1"),
                    CancellationToken.None))
                .ReturnsAsync(ApplicationResult<PublicLiveForecastResult>.Success(forecastResult));
        }
        PublicLiveController controller = new PublicLiveController(
            park.Object,
            item.Object,
            items.Object,
            history.Object,
            forecast.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        return controller;
    }

    private static PublicLiveTargetResult CreateTarget()
    {
        DateTime asOfUtc = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        return new PublicLiveTargetResult(
            "park-1",
            LiveTargetType.Park,
            "Park",
            "park-1",
            "Park",
            PublicLiveAvailability.Current,
            LiveOperationalStatus.Open,
            new[]
            {
                new PublicLiveQueueResult(
                    LiveQueueKind.Standby,
                    0,
                    false,
                    LiveQueueAvailability.Unspecified,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
            },
            asOfUtc,
            asOfUtc.AddMinutes(-2),
            asOfUtc.AddMinutes(-2).AddSeconds(1),
            120,
            LiveFreshnessState.Fresh,
            asOfUtc.AddMinutes(28),
            asOfUtc.AddMinutes(8),
            new PublicLiveSourceResult(
                "themeparks-wiki",
                "ThemeParks.wiki",
                LiveDataSourceType.AuthorizedAggregator,
                "Powered by ThemeParks.wiki",
                "https://themeparks.wiki/"),
            LiveDataConfidence.Medium);
    }

    private static PublicLiveHistoryResult CreateHistory()
    {
        DateTime toUtc = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        return new PublicLiveHistoryResult(
            "item-1",
            "Attraction",
            "park-1",
            "Park",
            toUtc.AddDays(-30),
            toUtc,
            "Europe/Paris",
            LiveWaitHistoryDataStatus.Insufficient,
            100,
            2,
            1,
            1,
            1,
            2d,
            0,
            new PublicLiveHistoryExclusionsResult(0, 0, 1, 0, 1),
            new[]
            {
                new PublicLiveHistoryHourResult(
                    10,
                    LiveWaitHistoryDataStatus.Insufficient,
                    10,
                    2,
                    1,
                    1,
                    1,
                    20d,
                    15d,
                    15d,
                    15d,
                    15d,
                    15d),
            },
            new PublicLiveSourceResult(
                "themeparks-wiki",
                "ThemeParks.wiki",
                LiveDataSourceType.AuthorizedAggregator,
                "Powered by ThemeParks.wiki",
                "https://themeparks.wiki/"));
    }

    private static PublicLiveForecastResult CreateForecast()
    {
        DateTime calculatedAtUtc = new DateTime(2026, 9, 29, 12, 30, 0, DateTimeKind.Utc);
        return new PublicLiveForecastResult(
            "Attraction",
            "Park",
            "Europe/Paris",
            new LiveWaitForecast(
                calculatedAtUtc.AddMinutes(30),
                calculatedAtUtc.AddMinutes(90),
                calculatedAtUtc,
                25d,
                15d,
                35d,
                12),
            LiveWaitForecastBacktestPolicy.StudyVersion,
            LiveWaitForecastBacktestPolicy.CandidateMethod,
            LiveWaitForecastBacktestPolicy.IntervalMethod,
            4d,
            82d,
            calculatedAtUtc.AddDays(-90),
            calculatedAtUtc,
            120,
            new PublicLiveSourceResult(
                "themeparks-wiki",
                "ThemeParks.wiki",
                LiveDataSourceType.AuthorizedAggregator,
                "Powered by ThemeParks.wiki",
                "https://themeparks.wiki/"));
    }
}
