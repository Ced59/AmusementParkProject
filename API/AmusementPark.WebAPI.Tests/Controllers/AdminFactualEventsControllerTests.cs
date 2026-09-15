using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Commands;
using AmusementPark.Application.Features.FactualEvents.Queries;
using AmusementPark.Application.Features.FactualEvents.Results;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.FactualEvents;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminFactualEventsControllerTests
{
    private static readonly DateTime OccurredAtUtc =
        new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SearchAsync_ShouldReturnReadableNamesWithoutTargetTechnicalIdentifiers()
    {
        FactualChangeEventAdminResult factualEvent = CreateResult();
        Mock<IQueryHandler<GetFactualChangeEventsQuery,
            ApplicationResult<PagedResult<FactualChangeEventAdminResult>>>> queryHandler =
            new Mock<IQueryHandler<GetFactualChangeEventsQuery,
                ApplicationResult<PagedResult<FactualChangeEventAdminResult>>>>(MockBehavior.Strict);
        queryHandler.Setup(value => value.HandleAsync(
                It.Is<GetFactualChangeEventsQuery>(query =>
                    query.Criteria.Status == FactualChangeStatus.Draft
                    && query.Criteria.TargetType == FactualTargetType.Park),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PagedResult<FactualChangeEventAdminResult>>.Success(
                new PagedResult<FactualChangeEventAdminResult>(
                    new[] { factualEvent },
                    1,
                    20,
                    1)));
        AdminFactualEventsController controller = CreateController(queryHandler.Object);

        IActionResult response = await controller.SearchAsync(
            new FactualChangeEventSearchRequestDto
            {
                Status = "Draft",
                TargetType = "Park",
            },
            CancellationToken.None);

        PagedResponseDto<FactualChangeEventAdminDto> body =
            Assert.IsType<PagedResponseDto<FactualChangeEventAdminDto>>(
                Assert.IsType<OkObjectResult>(response).Value);
        FactualChangeEventAdminDto dto = Assert.Single(body.Data);
        Assert.Equal("Parc exemple", dto.Target.Name);
        Assert.Null(typeof(FactualChangeTargetAdminDto).GetProperty("TargetId"));
        Assert.Null(typeof(FactualChangeTargetAdminDto).GetProperty("ParentParkId"));
        Assert.False(dto.CanBeDistributed);
        queryHandler.VerifyAll();
    }

    [Fact]
    public async Task VerifyAsync_ShouldForwardExpectedVersion()
    {
        Mock<ICommandHandler<VerifyFactualChangeEventCommand, ApplicationResult>> handler =
            new Mock<ICommandHandler<VerifyFactualChangeEventCommand, ApplicationResult>>(
                MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<VerifyFactualChangeEventCommand>(command =>
                    command.EventId == "event-1"
                    && command.ExpectedVersion == 3),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult.Success());
        AdminFactualEventsController controller = CreateController(verifyHandler: handler.Object);

        IActionResult response = await controller.VerifyAsync(
            "event-1",
            new FactualChangeEventMutationRequestDto { ExpectedVersion = 3 },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(response);
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldBeAdminOnlyNoStoreRateLimitedAndAudited()
    {
        AuthorizeAttribute authorization = Assert.Single(
            typeof(AdminFactualEventsController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => !string.IsNullOrWhiteSpace(attribute.Roles));
        RequireActivatedUnblockedUserAttribute activation = Assert.Single(
            typeof(AdminFactualEventsController)
                .GetCustomAttributes<RequireActivatedUnblockedUserAttribute>());
        MethodInfo searchAction = typeof(AdminFactualEventsController)
            .GetMethod(nameof(AdminFactualEventsController.SearchAsync))!;
        MethodInfo verifyAction = typeof(AdminFactualEventsController)
            .GetMethod(nameof(AdminFactualEventsController.VerifyAsync))!;
        MethodInfo publishAction = typeof(AdminFactualEventsController)
            .GetMethod(nameof(AdminFactualEventsController.PublishAsync))!;

        Assert.Equal(AuthorizationRoleGroups.Admin, authorization.Roles);
        Assert.Equal(AuthorizationPolicyNames.ActivatedUnblockedUser, activation.Policy);
        Assert.True(Assert.Single(searchAction.GetCustomAttributes<ResponseCacheAttribute>()).NoStore);
        Assert.Equal(
            RateLimitPolicyNames.FactualEventAdministration,
            Assert.Single(verifyAction.GetCustomAttributes<EnableRateLimitingAttribute>()).PolicyName);
        Assert.Equal(
            "factual-event.verify",
            Assert.Single(verifyAction.GetCustomAttributes<AdminAuditAttribute>()).Action);
        Assert.Equal(
            RateLimitPolicyNames.FactualEventAdministration,
            Assert.Single(publishAction.GetCustomAttributes<EnableRateLimitingAttribute>()).PolicyName);
        Assert.Equal(
            "factual-event.publish",
            Assert.Single(publishAction.GetCustomAttributes<AdminAuditAttribute>()).Action);
    }

    private static AdminFactualEventsController CreateController(
        IQueryHandler<GetFactualChangeEventsQuery,
            ApplicationResult<PagedResult<FactualChangeEventAdminResult>>>? queryHandler = null,
        ICommandHandler<VerifyFactualChangeEventCommand, ApplicationResult>? verifyHandler = null,
        ICommandHandler<PublishFactualChangeEventCommand, ApplicationResult>? publishHandler = null)
    {
        return new AdminFactualEventsController(
            queryHandler ?? Mock.Of<IQueryHandler<GetFactualChangeEventsQuery,
                ApplicationResult<PagedResult<FactualChangeEventAdminResult>>>>(),
            verifyHandler ?? Mock.Of<ICommandHandler<VerifyFactualChangeEventCommand, ApplicationResult>>(),
            publishHandler ?? Mock.Of<ICommandHandler<PublishFactualChangeEventCommand, ApplicationResult>>());
    }

    private static FactualChangeEventAdminResult CreateResult()
    {
        return new FactualChangeEventAdminResult(
            "event-1",
            FactualEventType.OpeningCalendarPublished,
            1,
            new FactualChangeTargetAdminResult(
                FactualTargetType.Park,
                "park-1",
                "Parc exemple",
                null,
                null),
            null,
            new FactualFactValueAdminResult(
                FactValueKind.Text,
                "timezone=Europe/Paris;coverage=2026-01-01/2026-12-31;rules=4;overrides=2;sha256=secret",
                null),
            new FactualSourceReferenceAdminResult(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Calendrier officiel",
                "https://example.com/calendar",
                OccurredAtUtc.AddHours(-1)),
            DataConfidence.High,
            OccurredAtUtc,
            1,
            FactualChangeStatus.Draft,
            OccurredAtUtc,
            OccurredAtUtc,
            null,
            null,
            1,
            false);
    }
}
