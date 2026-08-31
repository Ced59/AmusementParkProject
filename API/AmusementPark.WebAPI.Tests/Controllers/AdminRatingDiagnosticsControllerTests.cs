using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Ratings;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminRatingDiagnosticsControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldMapTheReadOnlyDiagnosticReport()
    {
        RatingDiagnosticsResult diagnostics = new RatingDiagnosticsResult(
            new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc),
            51,
            24,
            10,
            new[] { "0.5", "1" },
            false,
            new RatingAnomalySummaryResult(0, 0, 0, 0, 0, 0, 0, 0, 0),
            new RatingAggregateIntegrityResult(true, true, 8, 0, 0, 0, 0, 0),
            new[] { new RatingTargetDistributionResult("Park", "3-9", 2, 12, 12) },
            new[] { new RatingIndexStatusResult("userRatings", "idx", true, true, false, false, true, true, "{ userId: 1 }", "{ userId: 1 }") });
        Mock<IQueryHandler<GetRatingDiagnosticsQuery, ApplicationResult<RatingDiagnosticsResult>>> handler =
            new Mock<IQueryHandler<GetRatingDiagnosticsQuery, ApplicationResult<RatingDiagnosticsResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(It.IsAny<GetRatingDiagnosticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<RatingDiagnosticsResult>.Success(diagnostics));
        AdminRatingDiagnosticsController controller = new AdminRatingDiagnosticsController(handler.Object);

        IActionResult response = await controller.GetAsync(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(response);
        RatingDiagnosticsDto dto = Assert.IsType<RatingDiagnosticsDto>(ok.Value);
        Assert.Equal(24, dto.TotalRatings);
        Assert.Equal("3-9", Assert.Single(dto.TargetDistribution).EvidenceBand);
        Assert.True(dto.AggregateIntegrity.IsSourceComparisonEvaluated);
        Assert.True(dto.AggregateIntegrity.IsOrphanCheckEvaluated);
        Assert.True(Assert.Single(dto.Indexes).MatchesExpectedDefinition);
        Assert.False(Assert.Single(dto.Indexes).IsHidden);
        Assert.False(Assert.Single(dto.Indexes).HasUnexpectedOptions);
        Assert.True(Assert.Single(dto.Indexes).SupportsExpectedQueries);
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldRequireAdminAndSerializeDiagnosticRuns()
    {
        AuthorizeAttribute authorize = Assert.Single(
            typeof(AdminRatingDiagnosticsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => !string.IsNullOrWhiteSpace(attribute.Roles));
        MethodInfo action = typeof(AdminRatingDiagnosticsController).GetMethod(nameof(AdminRatingDiagnosticsController.GetAsync))!;
        EnableRateLimitingAttribute rateLimit = Assert.Single(action.GetCustomAttributes<EnableRateLimitingAttribute>());

        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.Equal(RateLimitPolicyNames.RatingDiagnostics, rateLimit.PolicyName);
    }
}
