using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankingRebuildNotifierTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task NotifyMutationAsync_WhenParkItemChanges_ShouldScheduleCategoryAndComposedParkScopes()
    {
        List<CoalesceBackgroundJobRequest> requests = new List<CoalesceBackgroundJobRequest>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.IncrementAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .ReturnsAsync((RankingScopeKey scopeKey, CancellationToken _) =>
                new RatingRankingSourceRevision(
                    scopeKey,
                    string.Equals(scopeKey.Value, "parks:global", StringComparison.Ordinal) ? 18 : 12,
                    NowUtc));
        Mock<IDurableBackgroundJobRepository> jobs = new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs
            .Setup(repository => repository.CoalesceAsync(
                It.IsAny<CoalesceBackgroundJobRequest>(),
                CancellationToken.None))
            .Callback((CoalesceBackgroundJobRequest request, CancellationToken _) => requests.Add(request))
            .ReturnsAsync((DurableBackgroundJob)null!);
        RatingRankingRebuildNotifier notifier = CreateNotifier(revisions.Object, jobs.Object);

        await notifier.NotifyMutationAsync(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction,
            CancellationToken.None);

        Assert.Equal(2, requests.Count);
        Assert.Equal(
            new[] { "park-items:category:attraction", "parks:global" },
            requests.Select(static request => request.Payload.GetProperty("scopeKey").GetString()));
        Assert.All(requests, static request =>
        {
            Assert.Equal(RatingRankingRebuildJobContract.Kind, request.Kind);
            Assert.Equal(RatingRankingRebuildJobContract.PayloadVersion, request.PayloadVersion);
            Assert.Equal(
                $"ratings.rebuild-scope:{request.Payload.GetProperty("scopeKey").GetString()}",
                request.NaturalKey);
            Assert.Equal(
                request.RequestedRevision,
                request.Payload.GetProperty("requestedSourceRevision").GetInt64());
            Assert.Equal(
                RankingEligibilityPolicy.InitialMethodologyVersion.Value,
                request.Payload.GetProperty("methodologyVersion").GetString());
        });
        Assert.Equal(new long[] { 12, 18 }, requests.Select(static request => request.RequestedRevision));
        revisions.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task NotifyMutationAsync_WhenCategorySchedulingFails_ShouldStillScheduleTheGlobalParkScope()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.IncrementAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .ReturnsAsync((RankingScopeKey scopeKey, CancellationToken _) =>
                new RatingRankingSourceRevision(scopeKey, 7, NowUtc));
        Mock<IDurableBackgroundJobRepository> jobs = new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs
            .Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.NaturalKey == "ratings.rebuild-scope:park-items:category:attraction"),
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        jobs
            .Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.NaturalKey == "ratings.rebuild-scope:parks:global"),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        RatingRankingRebuildNotifier notifier = CreateNotifier(revisions.Object, jobs.Object);

        await notifier.NotifyMutationAsync(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction,
            CancellationToken.None);

        revisions.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task NotifyMutationAsync_WhenParkChanges_ShouldOnlyScheduleTheGlobalParkScope()
    {
        RankingScopeKey globalScopeKey = RankingScopeKey.Parse("parks:global");
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.IncrementAsync(globalScopeKey, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(globalScopeKey, 3, NowUtc));
        Mock<IDurableBackgroundJobRepository> jobs = new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs
            .Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.NaturalKey == "ratings.rebuild-scope:parks:global" &&
                    request.RequestedRevision == 3),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        RatingRankingRebuildNotifier notifier = CreateNotifier(revisions.Object, jobs.Object);

        await notifier.NotifyMutationAsync(RatingTargetType.Park, null, CancellationToken.None);

        revisions.VerifyAll();
        jobs.VerifyAll();
    }

    private static RatingRankingRebuildNotifier CreateNotifier(
        IRatingRankingSourceRevisionRepository revisions,
        IDurableBackgroundJobRepository jobs)
    {
        RankingScopeRegistry registry = new RankingScopeRegistry(
            CanonicalRankingScopes.Version,
            CanonicalRankingScopes.All);
        return new RatingRankingRebuildNotifier(
            registry,
            revisions,
            jobs,
            NullLogger<RatingRankingRebuildNotifier>.Instance);
    }
}
