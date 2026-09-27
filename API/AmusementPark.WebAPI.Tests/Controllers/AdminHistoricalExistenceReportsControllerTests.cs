using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Commands;
using AmusementPark.Application.Features.HistoricalExistenceReports.Queries;
using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminHistoricalExistenceReportsControllerTests
{
    [Fact]
    public void Controller_ShouldBeAdminOnlyAuditedAndRateLimited()
    {
        Type type = typeof(AdminHistoricalExistenceReportsController);
        Assert.Equal(
            "admin/history/existence-reports",
            type.GetCustomAttribute<RouteAttribute>()?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            type.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.True(type.GetCustomAttribute<ResponseCacheAttribute>()?.NoStore);

        MethodInfo review = Assert.IsAssignableFrom<MethodInfo>(
            type.GetMethod(nameof(AdminHistoricalExistenceReportsController.ReviewAsync)));
        Assert.Equal(
            RateLimitPolicyNames.HistoricalExistenceReportAdministration,
            review.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        AdminAuditAttribute audit = Assert.IsType<AdminAuditAttribute>(
            review.GetCustomAttribute<AdminAuditAttribute>());
        Assert.Equal("history.existence-report.review", audit.Action);
    }

    [Fact]
    public async Task ListAsync_WithInvalidStatus_ShouldRejectBeforeQuerying()
    {
        Mock<IQueryHandler<
            GetHistoricalExistenceReportsQuery,
            ApplicationResult<PagedResult<HistoricalExistenceReportResult>>>> query =
            new(MockBehavior.Strict);
        Mock<ICommandHandler<ReviewHistoricalExistenceReportCommand, ApplicationResult>> command =
            new(MockBehavior.Strict);
        AdminHistoricalExistenceReportsController controller = new(
            query.Object,
            command.Object);

        IActionResult result = await controller.ListAsync(
            new AmusementPark.WebAPI.Contracts.History.HistoricalExistenceReportSearchRequestDto
            {
                Status = "Published",
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        query.VerifyNoOtherCalls();
    }
}
