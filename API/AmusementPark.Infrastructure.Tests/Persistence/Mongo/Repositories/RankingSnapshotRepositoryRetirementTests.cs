using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class RankingSnapshotRepositoryRetirementTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsRetirementStale_WhenPointerHasNewerRevision_ShouldProtectPointer()
    {
        RankingPublicationPointer pointer = CreatePointer(
            RatingMethodologyVersion.Parse("ratings-2026-01"),
            8);
        RetireRankingPublicationRequest request = CreateRequest(
            RatingMethodologyVersion.Parse("ratings-2026-01"),
            7);

        bool result = RankingSnapshotRepository.IsRetirementStale(pointer, request);

        Assert.True(result);
    }

    [Fact]
    public void IsRetirementStale_WhenPointerCoversSameMethodologyAndRevision_ShouldProtectPointer()
    {
        RatingMethodologyVersion methodology = RatingMethodologyVersion.Parse("ratings-2026-01");
        RankingPublicationPointer pointer = CreatePointer(methodology, 7);
        RetireRankingPublicationRequest request = CreateRequest(methodology, 7);

        bool result = RankingSnapshotRepository.IsRetirementStale(pointer, request);

        Assert.True(result);
    }

    [Fact]
    public void IsRetirementStale_WhenSameRevisionBelongsToReplacedMethodology_ShouldAllowRetirement()
    {
        RankingPublicationPointer pointer = CreatePointer(
            RatingMethodologyVersion.Parse("ratings-2025-01"),
            7);
        RetireRankingPublicationRequest request = CreateRequest(
            RatingMethodologyVersion.Parse("ratings-2026-01"),
            7);

        bool result = RankingSnapshotRepository.IsRetirementStale(pointer, request);

        Assert.False(result);
    }

    private static RetireRankingPublicationRequest CreateRequest(
        RatingMethodologyVersion methodology,
        long revision)
    {
        return new RetireRankingPublicationRequest(
            RankingScopeKey.Parse("parks:global"),
            methodology,
            revision);
    }

    private static RankingPublicationPointer CreatePointer(
        RatingMethodologyVersion methodology,
        long revision)
    {
        return new RankingPublicationPointer(
            RankingScopeKey.Parse("parks:global"),
            RankingSnapshotId.Parse("snapshot-1"),
            NowUtc,
            null,
            null,
            methodology,
            revision,
            revision,
            1,
            NowUtc);
    }
}
