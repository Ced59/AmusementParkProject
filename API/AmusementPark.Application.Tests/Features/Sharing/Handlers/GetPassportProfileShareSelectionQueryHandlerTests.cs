using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Handlers;

public sealed class GetPassportProfileShareSelectionQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldFilterVisitedPublicParksBeforeRatingLimit()
    {
        Mock<IPassportProfileSourceReader> sourceReader = new Mock<IPassportProfileSourceReader>(MockBehavior.Strict);
        sourceReader.Setup(value => value.ReadOwnedCompletedVisitsAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-1",
                    "park-visited",
                    VisitDate.ForDay(2026, 8, 12),
                    null),
            });
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(value => value.GetByIdsAsync(
                It.Is<IEnumerable<string>>(parkIds => parkIds.SequenceEqual(new[] { "park-visited" })),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-visited",
                    Name = "Parc visité",
                    IsVisible = true,
                },
            });
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(value => value.GetVisibleUserRankingSourcesForParksAsync(
                "owner-1",
                It.Is<IReadOnlyCollection<string>>(parkIds => parkIds.SequenceEqual(new[] { "park-visited" })),
                100,
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new UserRatingListItemResult(
                    "rating-1",
                    RatingTargetType.Park,
                    "park-visited",
                    "Parc visité",
                    "park-visited",
                    "Parc visité",
                    null,
                    null,
                    4.5,
                    new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc),
                    new RatingSummaryResult(RatingTargetType.Park, "park-visited", 1, 4.5, 4.5)),
            });
        Mock<ISharePublicationRepository> publications = new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetOwnedBySourceAsync(
                "owner-1",
                SharePublicationType.PassportProfile,
                PassportProfileShareSourceScope.Create("owner-1"),
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        Mock<IPassportProfileShareSnapshotRepository> snapshots =
            new Mock<IPassportProfileShareSnapshotRepository>(MockBehavior.Strict);
        GetPassportProfileShareSelectionQueryHandler handler = new GetPassportProfileShareSelectionQueryHandler(
            sourceReader.Object,
            parks.Object,
            ratings.Object,
            publications.Object,
            snapshots.Object);

        ApplicationResult<PassportProfileShareSelectionResult> result = await handler.HandleAsync(
            new GetPassportProfileShareSelectionQuery("owner-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        PassportProfileShareRatingCandidateResult rating = Assert.Single(result.Value!.Ratings);
        Assert.Equal("Parc visité", rating.Name);
        Assert.Equal(250, result.Value.MaximumSelectedParks);
        ratings.VerifyAll();
    }
}
