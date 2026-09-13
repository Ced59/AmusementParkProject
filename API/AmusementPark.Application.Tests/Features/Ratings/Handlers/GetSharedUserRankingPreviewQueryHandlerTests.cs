using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class GetSharedUserRankingPreviewQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPublicationVersionIsStale_ShouldNotReadRankingData()
    {
        PreviewHandlerFixture fixture = CreateFixture();

        ApplicationResult<UserRankingSharePreviewFileResult> result = await fixture.Handler.HandleAsync(
            new GetSharedUserRankingPreviewQuery(
                "share-1",
                PublicationVersion: 6,
                TemplateVersion: 1,
                Language: "fr"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.social-image-not-found");
        fixture.ParkRankings.VerifyNoOtherCalls();
        fixture.ParkItemRankings.VerifyNoOtherCalls();
        fixture.Renderer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(2, "fr", "share-publication.social-image-not-found")]
    [InlineData(1, "ja", "share-publication.social-image-language-invalid")]
    public async Task HandleAsync_WhenSocialImageVariantIsUnsupported_ShouldNotRender(
        int templateVersion,
        string language,
        string expectedErrorCode)
    {
        PreviewHandlerFixture fixture = CreateFixture();

        ApplicationResult<UserRankingSharePreviewFileResult> result = await fixture.Handler.HandleAsync(
            new GetSharedUserRankingPreviewQuery(
                "share-1",
                PublicationVersion: 7,
                TemplateVersion: templateVersion,
                Language: language),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == expectedErrorCode);
        fixture.ParkRankings.VerifyNoOtherCalls();
        fixture.ParkItemRankings.VerifyNoOtherCalls();
        fixture.Renderer.VerifyNoOtherCalls();
    }

    private static PreviewHandlerFixture CreateFixture()
    {
        ResolvedSharePublicationResult publication = new ResolvedSharePublicationResult(
            "owner-1",
            "Camille",
            SharePublicationType.PersonalRanking,
            ShareContentPolicy.Create(
                SharePublicationType.PersonalRanking,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.GlobalRatings }),
            new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc),
            "personal-ranking:owner-1",
            4,
            7);
        Mock<ISharePublicationAccessResolver> resolver =
            new Mock<ISharePublicationAccessResolver>(MockBehavior.Strict);
        resolver.Setup(value => value.ResolveAsync(
                "share-1",
                SharePublicationType.PersonalRanking,
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ResolvedSharePublicationResult>.Success(publication));
        Mock<IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>>> parkRankings =
            new Mock<IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>>>(MockBehavior.Strict);
        Mock<IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>> parkItemRankings =
            new Mock<IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>>(MockBehavior.Strict);
        Mock<IUserRankingSharePreviewRenderer> renderer =
            new Mock<IUserRankingSharePreviewRenderer>(MockBehavior.Strict);
        GetSharedUserRankingPreviewQueryHandler handler = new GetSharedUserRankingPreviewQueryHandler(
            resolver.Object,
            parkRankings.Object,
            parkItemRankings.Object,
            renderer.Object);
        return new PreviewHandlerFixture(handler, parkRankings, parkItemRankings, renderer);
    }
}
