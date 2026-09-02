using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Ratings;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminRatingRankingManagementControllerTests
{
    [Fact]
    public async Task PreviewAsync_ShouldMapTheCompleteCandidateAndImpact()
    {
        RatingRankingPolicyCandidate candidate = CreateCandidate();
        RatingRankingPolicyImpactResult impact = new RatingRankingPolicyImpactResult(
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc),
            candidate,
            2,
            1,
            7,
            9,
            1.25,
            3,
            1,
            2,
            90,
            4,
            Array.Empty<RatingRankingPolicyScopeImpactResult>());
        Mock<IQueryHandler<PreviewRatingRankingPolicyImpactQuery,
            ApplicationResult<RatingRankingPolicyImpactResult>>> previewHandler =
            new Mock<IQueryHandler<PreviewRatingRankingPolicyImpactQuery,
                ApplicationResult<RatingRankingPolicyImpactResult>>>(MockBehavior.Strict);
        previewHandler.Setup(handler => handler.HandleAsync(
                It.Is<PreviewRatingRankingPolicyImpactQuery>(query =>
                    query.Candidate.Version == candidate.Version
                    && query.Candidate.EligibleMinUniqueContributors
                    == candidate.EligibleMinUniqueContributors
                    && query.Candidate.ScoreTieEpsilon == candidate.ScoreTieEpsilon),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<RatingRankingPolicyImpactResult>.Success(impact));
        AdminRatingRankingManagementController controller = CreateController(
            previewHandler: previewHandler.Object);

        IActionResult response = await controller.PreviewAsync(
            ToDto(candidate),
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(response);
        RatingRankingPolicyImpactDto dto = Assert.IsType<RatingRankingPolicyImpactDto>(ok.Value);
        Assert.Equal(7, dto.ComparedRankCount);
        Assert.Equal(9, dto.TotalAbsoluteRankChange);
        Assert.Equal(1.25, dto.AverageRankChange);
        Assert.Equal(candidate.Version, dto.Candidate.Version);
        previewHandler.VerifyAll();
    }

    [Fact]
    public async Task RebuildAsync_ShouldForwardConfirmationAndReturnAccepted()
    {
        RatingRankingRebuildRequestResult rebuild = new RatingRankingRebuildRequestResult(
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc),
            1,
            new[] { new RatingRankingScheduledScopeResult("parks:global", 8) });
        Mock<ICommandHandler<RebuildRatingRankingSnapshotsCommand,
            ApplicationResult<RatingRankingRebuildRequestResult>>> rebuildHandler =
            new Mock<ICommandHandler<RebuildRatingRankingSnapshotsCommand,
                ApplicationResult<RatingRankingRebuildRequestResult>>>(MockBehavior.Strict);
        rebuildHandler.Setup(handler => handler.HandleAsync(
                It.Is<RebuildRatingRankingSnapshotsCommand>(command => command.Confirmed),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<RatingRankingRebuildRequestResult>.Success(rebuild));
        AdminRatingRankingManagementController controller = CreateController(
            rebuildHandler: rebuildHandler.Object);

        IActionResult response = await controller.RebuildAsync(
            new RatingRankingRebuildRequestDto { Confirmed = true },
            CancellationToken.None);

        AcceptedResult accepted = Assert.IsType<AcceptedResult>(response);
        RatingRankingRebuildRequestResultDto dto =
            Assert.IsType<RatingRankingRebuildRequestResultDto>(accepted.Value);
        Assert.Equal(8, Assert.Single(dto.Scopes).RequestedSourceRevision);
        rebuildHandler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldBeProtectedRateLimitedAndAuditMutations()
    {
        AuthorizeAttribute roleAuthorization = Assert.Single(
            typeof(AdminRatingRankingManagementController)
                .GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => !string.IsNullOrWhiteSpace(attribute.Roles));
        RequireActivatedUnblockedUserAttribute activation = Assert.Single(
            typeof(AdminRatingRankingManagementController)
                .GetCustomAttributes<RequireActivatedUnblockedUserAttribute>());
        EnableRateLimitingAttribute rateLimit = Assert.Single(
            typeof(AdminRatingRankingManagementController)
                .GetCustomAttributes<EnableRateLimitingAttribute>());
        MethodInfo previewAction = typeof(AdminRatingRankingManagementController)
            .GetMethod(nameof(AdminRatingRankingManagementController.PreviewAsync))!;
        MethodInfo rebuildAction = typeof(AdminRatingRankingManagementController)
            .GetMethod(nameof(AdminRatingRankingManagementController.RebuildAsync))!;
        AdminAuditAttribute previewAudit = Assert.Single(
            previewAction.GetCustomAttributes<AdminAuditAttribute>());
        AdminAuditAttribute rebuildAudit = Assert.Single(
            rebuildAction.GetCustomAttributes<AdminAuditAttribute>());

        Assert.Equal(AuthorizationRoleGroups.Admin, roleAuthorization.Roles);
        Assert.Equal(AuthorizationPolicyNames.ActivatedUnblockedUser, activation.Policy);
        Assert.Equal(RateLimitPolicyNames.RatingDiagnostics, rateLimit.PolicyName);
        Assert.Equal("rating-ranking.policy.preview", previewAudit.Action);
        Assert.Equal("candidate", previewAudit.StaticTargetId);
        Assert.Equal("rating-ranking.snapshots.rebuild", rebuildAudit.Action);
        Assert.Equal("all", rebuildAudit.StaticTargetId);
    }

    private static AdminRatingRankingManagementController CreateController(
        IQueryHandler<PreviewRatingRankingPolicyImpactQuery,
            ApplicationResult<RatingRankingPolicyImpactResult>>? previewHandler = null,
        ICommandHandler<RebuildRatingRankingSnapshotsCommand,
            ApplicationResult<RatingRankingRebuildRequestResult>>? rebuildHandler = null)
    {
        return new AdminRatingRankingManagementController(
            Mock.Of<IQueryHandler<GetRatingRankingAdministrationQuery,
                ApplicationResult<RatingRankingAdministrationResult>>>(),
            previewHandler ?? Mock.Of<IQueryHandler<PreviewRatingRankingPolicyImpactQuery,
                ApplicationResult<RatingRankingPolicyImpactResult>>>(),
            rebuildHandler ?? Mock.Of<ICommandHandler<RebuildRatingRankingSnapshotsCommand,
                ApplicationResult<RatingRankingRebuildRequestResult>>>());
    }

    private static RatingRankingPolicyCandidate CreateCandidate()
    {
        return new RatingRankingPolicyCandidate(
            "ratings-2026-02",
            3,
            10,
            30,
            100,
            3,
            5,
            2,
            2,
            0.0001m);
    }

    private static RatingRankingPolicyCandidateRequestDto ToDto(
        RatingRankingPolicyCandidate candidate)
    {
        return new RatingRankingPolicyCandidateRequestDto
        {
            Version = candidate.Version,
            ProvisionalMinUniqueContributors = candidate.ProvisionalMinUniqueContributors,
            EligibleMinUniqueContributors = candidate.EligibleMinUniqueContributors,
            EstablishedMinUniqueContributors = candidate.EstablishedMinUniqueContributors,
            StrongEvidenceMinUniqueContributors = candidate.StrongEvidenceMinUniqueContributors,
            MinimumEligibleEntriesPerRanking = candidate.MinimumEligibleEntriesPerRanking,
            MinimumEligibleItemsForParkItemComponent = candidate.MinimumEligibleItemsForParkItemComponent,
            MinimumEligibleItemsPerCategory = candidate.MinimumEligibleItemsPerCategory,
            MinimumEligibleCategories = candidate.MinimumEligibleCategories,
            ScoreTieEpsilon = candidate.ScoreTieEpsilon,
        };
    }
}
