using System.Net;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Services.LiveData;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.LiveData;

public sealed class ThemeParksWikiLiveDataAdapterTests
{
    [Fact]
    public async Task FetchLatestAsync_WithCompleteFixture_ShouldNormalizeAllStatusesAndQueues()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = ThemeParksWikiFixtureLoader.Read("complete.json"),
            EntityTag = "\"complete-v1\"",
        };
        ThemeParksWikiLiveDataAdapter adapter = CreateAdapter(handler);

        LiveProviderReadResult result = await adapter.FetchLatestAsync(
            new LiveProviderReadRequest("park/root"),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.Success, result.Disposition);
        Assert.Equal("themeparks-wiki", adapter.SourceId.Value);
        Assert.Equal("\"complete-v1\"", result.EntityTag);
        Assert.Equal(64, result.PayloadSha256?.Length);
        Assert.Equal(4, result.Observations.Count);
        Assert.Empty(result.Diagnostics);
        ExternalLiveObservation operating = result.Observations.Single(observation =>
            observation.ExternalTargetId == "attraction-operating");
        Assert.Equal(LiveOperationalStatus.Open, operating.Status);
        Assert.Equal(6, operating.Queues.Count);
        Assert.Equal(0, operating.Queues.Single(queue => queue.Kind == LiveQueueKind.Standby).WaitTimeMinutes);
        Assert.Null(operating.Queues.Single(queue => queue.Kind == LiveQueueKind.SingleRider).WaitTimeMinutes);
        Assert.Equal(
            LiveQueueAvailability.TemporarilyFull,
            operating.Queues.Single(queue => queue.Kind == LiveQueueKind.PaidReturnTime).Availability);
        Assert.Equal(
            0,
            operating.Queues.Single(queue => queue.Kind == LiveQueueKind.PaidReturnTime).PriceMinorUnits);
        Assert.True(operating.Queues.Single(queue => queue.Kind == LiveQueueKind.BoardingGroup).IsEstimated);
        Assert.Contains(result.Observations, static observation =>
            observation.Status == LiveOperationalStatus.Down && observation.TargetType == LiveTargetType.Park);
        Assert.Contains(result.Observations, static observation =>
            observation.Status == LiveOperationalStatus.Closed);
        Assert.Contains(result.Observations, static observation =>
            observation.Status == LiveOperationalStatus.Maintenance);
        HttpRequestMessage sentRequest = Assert.Single(handler.Requests);
        Assert.Equal("/v1/entity/park%2Froot/live", sentRequest.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task FetchLatestAsync_WithUnknownFixture_ShouldUseSafeFallbacksAndDiagnostics()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = ThemeParksWikiFixtureLoader.Read("unknown-values.json"),
        };
        ThemeParksWikiLiveDataAdapter adapter = CreateAdapter(handler);

        LiveProviderReadResult result = await adapter.FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        ExternalLiveObservation observation = Assert.Single(result.Observations);
        Assert.Equal(LiveOperationalStatus.Unknown, observation.Status);
        Assert.Equal(
            LiveQueueAvailability.Unknown,
            Assert.Single(observation.Queues).Availability);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == LiveProviderDiagnosticCodes.UnknownStatus);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == LiveProviderDiagnosticCodes.UnknownQueueState);
        Assert.Equal(
            2,
            result.Diagnostics.Count(static diagnostic =>
                diagnostic.Code == LiveProviderDiagnosticCodes.UnknownQueueKind));
        Assert.All(
            result.Diagnostics.Where(static diagnostic =>
                diagnostic.Code == LiveProviderDiagnosticCodes.UnknownQueueKind),
            static diagnostic => Assert.True(diagnostic.Field?.Length <= 100));
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == LiveProviderDiagnosticCodes.UnsupportedEntityType);
        Assert.Equal(
            3,
            result.Diagnostics.Count(static diagnostic =>
                diagnostic.Code == LiveProviderDiagnosticCodes.InvalidObservation));
    }

    [Fact]
    public async Task FetchLatestAsync_WithInvalidQueues_ShouldNeverCoerceValuesToZero()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = ThemeParksWikiFixtureLoader.Read("invalid-queues.json"),
        };
        ThemeParksWikiLiveDataAdapter adapter = CreateAdapter(handler);

        LiveProviderReadResult result = await adapter.FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        ExternalLiveObservation observation = Assert.Single(result.Observations);
        Assert.DoesNotContain(observation.Queues, static queue => queue.WaitTimeMinutes == 0);
        Assert.DoesNotContain(observation.Queues, static queue => queue.Kind == LiveQueueKind.ReturnTime);
        Assert.True(result.Diagnostics.Count(diagnostic =>
            diagnostic.Code == LiveProviderDiagnosticCodes.InvalidQueueValue) >= 5);
    }

    [Fact]
    public async Task FetchLatestAsync_WhenRequiredQueueStringsAreBlank_ShouldRejectAffectedQueues()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = ThemeParksWikiFixtureLoader.Read("blank-required-queue-strings.json"),
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        ExternalLiveObservation observation = Assert.Single(result.Observations);
        Assert.Empty(observation.Queues);
        Assert.Equal(
            3,
            result.Diagnostics.Count(static diagnostic =>
                diagnostic.Code == LiveProviderDiagnosticCodes.InvalidQueueValue));
    }

    [Fact]
    public async Task FetchLatestAsync_WithEmptyFixture_ShouldReturnSuccessfulEmptyBatchWithDiagnostic()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = ThemeParksWikiFixtureLoader.Read("empty.json"),
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.Success, result.Disposition);
        Assert.Empty(result.Observations);
        Assert.Equal(
            LiveProviderDiagnosticCodes.EmptyResponse,
            Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public async Task FetchLatestAsync_WhenStandbyWaitIsOmittedBySchema_ShouldKeepUnknownWait()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = ThemeParksWikiFixtureLoader.Read("standby-without-wait.json"),
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        ExternalLiveObservation observation = Assert.Single(result.Observations);
        LiveQueueObservation queue = Assert.Single(observation.Queues);
        Assert.Equal(LiveQueueKind.Standby, queue.Kind);
        Assert.Null(queue.WaitTimeMinutes);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task FetchLatestAsync_WhenNotModified_ShouldForwardConditionalEntityTag()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            StatusCode = HttpStatusCode.NotModified,
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root", "\"previous-v1\""),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.NotModified, result.Disposition);
        Assert.Equal("\"previous-v1\"", result.EntityTag);
        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Contains(request.Headers.IfNoneMatch, static tag => tag.Tag == "\"previous-v1\"");
    }

    [Fact]
    public async Task FetchLatestAsync_WhenRateLimited_ShouldExposeRetryAfterWithoutRetrying()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            StatusCode = HttpStatusCode.TooManyRequests,
            RetryAfter = TimeSpan.FromSeconds(90),
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.RateLimited, result.Disposition);
        Assert.Equal(TimeSpan.FromSeconds(90), result.RetryAfter);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task FetchLatestAsync_WhenProviderRejectsRequest_ShouldReturnUnavailable(
        HttpStatusCode statusCode)
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            StatusCode = statusCode,
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.Unavailable, result.Disposition);
    }

    [Fact]
    public async Task FetchLatestAsync_WhenPayloadIsMalformed_ShouldReturnInvalidPayloadWithHash()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = "{not-json",
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.InvalidPayload, result.Disposition);
        Assert.Equal(64, result.PayloadSha256?.Length);
        Assert.Empty(result.Observations);
    }

    [Fact]
    public async Task FetchLatestAsync_WhenDeclaredPayloadIsTooLarge_ShouldRejectBeforeReading()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            DeclaredContentLength = ThemeParksWikiLiveDataAdapter.MaximumResponseBytes + 1L,
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.ResponseTooLarge, result.Disposition);
    }

    [Fact]
    public async Task FetchLatestAsync_WhenProviderTimesOut_ShouldReturnUnavailable()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            ThrowTimeout = true,
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.Unavailable, result.Disposition);
    }

    [Fact]
    public async Task FetchLatestAsync_WhenResponseBodyStalls_ShouldEnforceWholeRequestDeadline()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            ResponseStream = new StallingLiveDataReadStream(),
        };

        LiveProviderReadResult result = await CreateAdapter(
                handler,
                TimeSpan.FromMilliseconds(25))
            .FetchLatestAsync(
                new LiveProviderReadRequest("park-root"),
                CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.Unavailable, result.Disposition);
    }

    [Fact]
    public async Task FetchLatestAsync_WhenResponseStreamFails_ShouldReturnUnavailable()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            ResponseStream = new FailingLiveDataReadStream(),
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.Unavailable, result.Disposition);
    }

    private static ThemeParksWikiLiveDataAdapter CreateAdapter(
        ThemeParksWikiTestHttpMessageHandler handler,
        TimeSpan? requestTimeout = null)
    {
        HttpClient httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.themeparks.wiki/", UriKind.Absolute),
        };
        return requestTimeout.HasValue
            ? new ThemeParksWikiLiveDataAdapter(
                new LiveDataTestHttpClientFactory(httpClient),
                TimeProvider.System,
                requestTimeout.Value)
            : new ThemeParksWikiLiveDataAdapter(new LiveDataTestHttpClientFactory(httpClient));
    }
}
