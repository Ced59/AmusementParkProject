using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class RatingRankingAdministrationHandlersTests
{
    [Fact]
    public async Task PreviewHandler_ShouldRejectAnAlreadyPublishedMethodologyVersion()
    {
        PreviewRatingRankingPolicyImpactQueryHandler handler =
            new PreviewRatingRankingPolicyImpactQueryHandler(CreatePreviewer());
        RatingRankingPolicyCandidate candidate = CreateCandidate("ratings-2026-01");

        ApplicationResult<RatingRankingPolicyImpactResult> result = await handler.HandleAsync(
            new PreviewRatingRankingPolicyImpactQuery(candidate),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "rating.ranking-policy.version-already-published",
            Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task PreviewHandler_ShouldReturnAValidationErrorForInvalidThresholdOrdering()
    {
        PreviewRatingRankingPolicyImpactQueryHandler handler =
            new PreviewRatingRankingPolicyImpactQueryHandler(CreatePreviewer());
        RatingRankingPolicyCandidate candidate = CreateCandidate("ratings-2026-02") with
        {
            EligibleMinUniqueContributors = 2,
        };

        ApplicationResult<RatingRankingPolicyImpactResult> result = await handler.HandleAsync(
            new PreviewRatingRankingPolicyImpactQuery(candidate),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("rating.ranking-policy.invalid", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task RebuildHandler_ShouldRequireExplicitConfirmation()
    {
        RebuildRatingRankingSnapshotsCommandHandler handler =
            new RebuildRatingRankingSnapshotsCommandHandler(CreateRebuildRequester());

        ApplicationResult<RatingRankingRebuildRequestResult> result = await handler.HandleAsync(
            new RebuildRatingRankingSnapshotsCommand(false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "rating.ranking-rebuild.confirmation-required",
            Assert.Single(result.Errors).Code);
    }

    private static RatingRankingPolicyImpactPreviewer CreatePreviewer()
    {
        Mock<IRankingScopeRegistry> scopeRegistry =
            new Mock<IRankingScopeRegistry>(MockBehavior.Strict);
        scopeRegistry.SetupGet(value => value.Definitions)
            .Returns(Array.Empty<AmusementPark.Core.Domain.Ratings.RankingScopeDefinition>());
        return new RatingRankingPolicyImpactPreviewer(
            scopeRegistry.Object,
            Mock.Of<IRankingSnapshotRepository>(),
            Mock.Of<IRatingRankingSourceRevisionRepository>(),
            Mock.Of<IRatingRankingPolicyEvaluationBuilder>());
    }

    private static RatingRankingRebuildRequester CreateRebuildRequester()
    {
        return new RatingRankingRebuildRequester(
            Mock.Of<IRankingScopeRegistry>(),
            Mock.Of<IRatingRankingSourceRevisionRepository>(),
            Mock.Of<IRatingRankingRebuildScheduler>());
    }

    private static RatingRankingPolicyCandidate CreateCandidate(string version)
    {
        return new RatingRankingPolicyCandidate(
            version,
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
}
