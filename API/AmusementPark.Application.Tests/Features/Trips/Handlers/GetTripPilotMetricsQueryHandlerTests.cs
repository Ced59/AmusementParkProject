using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Handlers;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips.Handlers;

public sealed class GetTripPilotMetricsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnOnlyAggregateMetrics()
    {
        DateTime nowUtc = new DateTime(2027, 4, 5, 10, 0, 0, DateTimeKind.Utc);
        TripPilotMetricsSnapshot snapshot = new(
            12,
            5,
            8,
            4,
            2,
            3,
            42,
            1,
            new Dictionary<string, long> { ["CandidateAdded"] = 9 });
        Mock<ITripPilotMetricsRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.ReadAsync(CancellationToken.None)).ReturnsAsync(snapshot);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(item => item.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        GetTripPilotMetricsQueryHandler handler = new(repository.Object, clock.Object);

        ApplicationResult<TripPilotMetricsResult> result = await handler.HandleAsync(
            new GetTripPilotMetricsQuery(),
            CancellationToken.None);

        TripPilotMetricsResult metrics = Assert.IsType<TripPilotMetricsResult>(result.Value);
        Assert.Equal(nowUtc, metrics.GeneratedAtUtc);
        Assert.Equal(5, metrics.CollaborativePlans);
        Assert.Equal(42, metrics.AuditEvents);
        Assert.Equal(2, metrics.ExpiredInvitations);
        Assert.DoesNotContain(metrics.GetType().GetProperties(), property =>
            property.Name.Contains("User", StringComparison.Ordinal)
            || property.Name.Contains("Title", StringComparison.Ordinal)
            || property.Name.Contains("Note", StringComparison.Ordinal));
        repository.VerifyAll();
    }
}
