using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.LiveData;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminLiveTargetMappingsControllerTests
{
    [Fact]
    public void LiveEnums_ShouldUseStableStringContracts()
    {
        Assert.Equal("\"Candidate\"", JsonSerializer.Serialize(LiveMappingStatusDto.Candidate));
        Assert.Equal("\"ParkItem\"", JsonSerializer.Serialize(LiveTargetTypeDto.ParkItem));
        Assert.Equal("\"Verify\"", JsonSerializer.Serialize(LiveTargetMappingDecisionDto.Verify));
    }

    [Fact]
    public void Controller_ShouldBeAdminOnlyActivatedAndNonCacheable()
    {
        Type type = typeof(AdminLiveTargetMappingsController);

        Assert.Equal(
            "admin/live/mappings",
            type.GetCustomAttribute<RouteAttribute>()?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            type.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.NotNull(type.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.True(type.GetCustomAttribute<ResponseCacheAttribute>()?.NoStore);
        MethodInfo review = type.GetMethod(nameof(AdminLiveTargetMappingsController.ReviewAsync))!;
        Assert.Equal(
            RateLimitPolicyNames.LiveDataAdministration,
            review.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.NotNull(review.GetCustomAttribute<AdminAuditAttribute>());
    }

    [Fact]
    public async Task ListAsync_ShouldReturnMappedPaginatedMappings()
    {
        LiveTargetMappingResult mapping = CreateResult();
        Mock<IQueryHandler<
            GetAdminLiveTargetMappingsQuery,
            ApplicationResult<PagedResult<LiveTargetMappingResult>>>> queryHandler = new(
                MockBehavior.Strict);
        queryHandler.Setup(candidate => candidate.HandleAsync(
                It.Is<GetAdminLiveTargetMappingsQuery>(query =>
                    query.Criteria.Page == 2
                    && query.Criteria.Status == LiveMappingStatus.Candidate
                    && query.Criteria.Search == "mamba"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<PagedResult<LiveTargetMappingResult>>.Success(
                new PagedResult<LiveTargetMappingResult>(new[] { mapping }, 2, 25, 26)));
        AdminLiveTargetMappingsController controller = CreateController(queryHandler.Object);

        IActionResult action = await controller.ListAsync(
            2,
            25,
            null,
            LiveMappingStatusDto.Candidate,
            null,
            "mamba",
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(action);
        PagedResponseDto<LiveTargetMappingDto> response =
            Assert.IsType<PagedResponseDto<LiveTargetMappingDto>>(ok.Value);
        Assert.Equal(26, response.Pagination?.TotalItems);
        LiveTargetMappingDto item = Assert.Single(response.Data);
        Assert.Equal("Black Mamba", item.ExternalTarget.DisplayName);
        Assert.False(item.IsEligibleForLiveUse);
        queryHandler.VerifyAll();
    }

    [Fact]
    public async Task ReviewAsync_ShouldUseAuthenticatedAdministratorAsReviewer()
    {
        LiveTargetMappingResult mapping = CreateResult();
        Mock<ICommandHandler<
            ReviewLiveTargetMappingCommand,
            ApplicationResult<LiveTargetMappingResult>>> reviewHandler = new(MockBehavior.Strict);
        reviewHandler.Setup(candidate => candidate.HandleAsync(
                It.Is<ReviewLiveTargetMappingCommand>(command =>
                    command.MappingId == mapping.MappingId
                    && command.ReviewerUserId == "admin-1"
                    && command.ExpectedRevision == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<LiveTargetMappingResult>.Success(mapping));
        AdminLiveTargetMappingsController controller = CreateController(reviewHandler: reviewHandler.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, "admin-1") },
                        "test")),
            },
        };

        IActionResult action = await controller.ReviewAsync(
            mapping.MappingId,
            new ReviewLiveTargetMappingRequestDto
            {
                ExpectedRevision = 1,
                Decision = LiveTargetMappingDecisionDto.Verify,
                InternalTargetId = "item-1",
                ParkId = "park-1",
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        reviewHandler.VerifyAll();
    }

    private static AdminLiveTargetMappingsController CreateController(
        IQueryHandler<
            GetAdminLiveTargetMappingsQuery,
            ApplicationResult<PagedResult<LiveTargetMappingResult>>>? queryHandler = null,
        ICommandHandler<
            CreateLiveTargetMappingCandidateCommand,
            ApplicationResult<LiveTargetMappingResult>>? createHandler = null,
        ICommandHandler<
            ReviewLiveTargetMappingCommand,
            ApplicationResult<LiveTargetMappingResult>>? reviewHandler = null)
    {
        return new AdminLiveTargetMappingsController(
            queryHandler ?? Mock.Of<IQueryHandler<
                GetAdminLiveTargetMappingsQuery,
                ApplicationResult<PagedResult<LiveTargetMappingResult>>>>(),
            createHandler ?? Mock.Of<ICommandHandler<
                CreateLiveTargetMappingCandidateCommand,
                ApplicationResult<LiveTargetMappingResult>>>(),
            reviewHandler ?? Mock.Of<ICommandHandler<
                ReviewLiveTargetMappingCommand,
                ApplicationResult<LiveTargetMappingResult>>>());
    }

    private static LiveTargetMappingResult CreateResult()
    {
        return new LiveTargetMappingResult(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "10000000000000000000000000000001:1",
            "themeparks-wiki",
            new LiveExternalTargetResult(
                LiveTargetType.ParkItem,
                "external-item-1",
                "external-park-1",
                "Black Mamba",
                "Phantasialand",
                "DE"),
            null,
            LiveMappingStatus.Candidate,
            LiveMappingConfidence.Low,
            new DateTime(2026, 9, 28, 17, 0, 0, DateTimeKind.Utc),
            null,
            1,
            null,
            null,
            null,
            new DateTime(2026, 9, 28, 17, 0, 0, DateTimeKind.Utc),
            false);
    }
}
