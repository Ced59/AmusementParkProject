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
    public async Task FetchLatestAsync_WhenQueueContainsCrossKindFields_ShouldIgnoreForeignFacts()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = ThemeParksWikiFixtureLoader.Read("cross-kind-fields.json"),
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        ExternalLiveObservation observation = Assert.Single(result.Observations);
        LiveQueueObservation standby = observation.Queues.Single(static queue =>
            queue.Kind == LiveQueueKind.Standby);
        Assert.Equal(5, standby.WaitTimeMinutes);
        Assert.Null(standby.ReturnStartUtc);
        Assert.Null(standby.PriceMinorUnits);

        LiveQueueObservation returnTime = observation.Queues.Single(static queue =>
            queue.Kind == LiveQueueKind.ReturnTime);
        Assert.Null(returnTime.WaitTimeMinutes);
        Assert.Equal(new DateTime(2026, 9, 28, 10, 30, 0, DateTimeKind.Utc), returnTime.ReturnStartUtc);
        Assert.Null(returnTime.PriceMinorUnits);

        LiveQueueObservation boardingGroup = observation.Queues.Single(static queue =>
            queue.Kind == LiveQueueKind.BoardingGroup);
        Assert.Equal(12, boardingGroup.WaitTimeMinutes);
        Assert.Null(boardingGroup.ReturnStartUtc);
        Assert.Null(boardingGroup.PriceMinorUnits);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task FetchLatestAsync_WhenItemsOrFieldsHaveWrongTypes_ShouldKeepValidSiblings()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = ThemeParksWikiFixtureLoader.Read("wrong-item-field-types.json"),
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        ExternalLiveObservation observation = Assert.Single(result.Observations);
        Assert.Equal("valid-attraction", observation.ExternalTargetId);
        Assert.Equal(
            7,
            result.Diagnostics.Count(static diagnostic =>
                diagnostic.Code == LiveProviderDiagnosticCodes.InvalidObservation));
    }

    [Fact]
    public async Task FetchLatestAsync_WhenTimestampIsNotRfc3339_ShouldKeepOnlyStrictDates()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = ThemeParksWikiFixtureLoader.Read("non-rfc3339-timestamps.json"),
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        ExternalLiveObservation observation = Assert.Single(result.Observations);
        Assert.Equal("valid-attraction", observation.ExternalTargetId);
        Assert.Equal(
            new DateTime(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc),
            observation.SourceUpdatedAtUtc);
        Assert.Equal(
            2,
            result.Diagnostics.Count(static diagnostic =>
                diagnostic.Code == LiveProviderDiagnosticCodes.InvalidObservation));
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
    public async Task FetchLatestAsync_WhenClosedTargetReportsWait_ShouldEmitConflictDiagnostic()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = ThemeParksWikiFixtureLoader.Read("closed-with-wait.json"),
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        ExternalLiveObservation observation = Assert.Single(result.Observations);
        Assert.Equal(LiveOperationalStatus.Closed, observation.Status);
        Assert.Equal(25, Assert.Single(observation.Queues).WaitTimeMinutes);
        Assert.Equal(
            LiveProviderDiagnosticCodes.StatusQueueConflict,
            Assert.Single(result.Diagnostics).Code);
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

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"liveData\":null}")]
    public async Task FetchLatestAsync_WhenRequiredLiveDataArrayIsAbsent_ShouldReturnInvalidPayload(
        string payload)
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = payload,
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.InvalidPayload, result.Disposition);
        Assert.Equal(64, result.PayloadSha256?.Length);
        Assert.Empty(result.Observations);
    }

    [Fact]
    public async Task FetchLatestAsync_WhenObservationCountExceedsBound_ShouldRejectBeforeNormalization()
    {
        string entries = string.Join(
            ',',
            Enumerable.Repeat("0", ThemeParksWikiLiveDataAdapter.MaximumObservationCount + 1));
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            Content = $"{{\"liveData\":[{entries}]}}",
        };

        LiveProviderReadResult result = await CreateAdapter(handler).FetchLatestAsync(
            new LiveProviderReadRequest("park-root"),
            CancellationToken.None);

        Assert.Equal(LiveProviderReadDisposition.InvalidPayload, result.Disposition);
        Assert.Empty(result.Observations);
        Assert.Empty(result.Diagnostics);
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

    [Fact]
    public async Task FetchLatestAsync_WhenCompressedResponseIsCorrupt_ShouldReturnUnavailable()
    {
        ThemeParksWikiTestHttpMessageHandler handler = new ThemeParksWikiTestHttpMessageHandler
        {
            ResponseStream = new InvalidCompressedLiveDataReadStream(),
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
