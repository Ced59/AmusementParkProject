using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class RatingMethodologyQueryHandlersTests
{
    [Fact]
    public async Task GetCurrent_ShouldReturnEveryPublicCalculationParameter()
    {
        GetCurrentRatingMethodologyQueryHandler handler = new GetCurrentRatingMethodologyQueryHandler();

        ApplicationResult<RatingMethodologyResult> result = await handler.HandleAsync(
            new GetCurrentRatingMethodologyQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        RatingMethodologyResult methodology = Assert.IsType<RatingMethodologyResult>(result.Value);
        Assert.Equal("ratings-2026-01", methodology.Version.ToString());
        Assert.True(methodology.IsCurrent);
        Assert.Equal(3, methodology.ProvisionalMinUniqueContributors);
        Assert.Equal(10, methodology.EligibleMinUniqueContributors);
        Assert.Equal(30, methodology.EstablishedMinUniqueContributors);
        Assert.Equal(100, methodology.StrongEvidenceMinUniqueContributors);
        Assert.Equal(3, methodology.MinimumEligibleEntriesPerRanking);
        Assert.Equal(5, methodology.MinimumEligibleItemsForParkItemComponent);
        Assert.Equal(2, methodology.MinimumEligibleItemsPerCategory);
        Assert.Equal(2, methodology.MinimumEligibleCategories);
        Assert.Equal(0.0001m, methodology.ScoreTieEpsilon);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ratings-2099-01")]
    public async Task GetByVersion_ShouldReturnNotFoundForAnUnknownVersion(string version)
    {
        GetRatingMethodologyQueryHandler handler = new GetRatingMethodologyQueryHandler();

        ApplicationResult<RatingMethodologyResult> result = await handler.HandleAsync(
            new GetRatingMethodologyQuery(version),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("rating.methodology.not-found", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task List_ShouldReturnTheHistoryNewestFirst()
    {
        ListRatingMethodologiesQueryHandler handler = new ListRatingMethodologiesQueryHandler();

        ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>> result = await handler.HandleAsync(
            new ListRatingMethodologiesQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ratings-2026-01", Assert.Single(result.Value!).Version.ToString());
    }
}
