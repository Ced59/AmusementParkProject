using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class GetRatingDiagnosticsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnTheReadOnlyInfrastructureReport()
    {
        RatingDiagnosticsResult diagnostics = CreateDiagnostics();
        Mock<IRatingDiagnosticsReader> reader = new Mock<IRatingDiagnosticsReader>(MockBehavior.Strict);
        reader.Setup(port => port.GetDiagnosticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(diagnostics);
        GetRatingDiagnosticsQueryHandler handler = new GetRatingDiagnosticsQueryHandler(reader.Object);

        ApplicationResult<RatingDiagnosticsResult> result = await handler.HandleAsync(
            new GetRatingDiagnosticsQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(diagnostics, result.Value);
        reader.VerifyAll();
    }

    private static RatingDiagnosticsResult CreateDiagnostics()
    {
        return new RatingDiagnosticsResult(
            new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc),
            42,
            12,
            10,
            new[] { "0.5", "1" },
            false,
            new RatingAnomalySummaryResult(0, 0, 0, 0, 0, 0, 0, 0, 0),
            new RatingAggregateIntegrityResult(3, 0, 0, 0),
            Array.Empty<RatingTargetDistributionResult>(),
            Array.Empty<RatingIndexStatusResult>());
    }
}
