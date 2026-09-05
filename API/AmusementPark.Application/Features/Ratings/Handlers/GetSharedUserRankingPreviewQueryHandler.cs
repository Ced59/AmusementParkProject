using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetSharedUserRankingPreviewQueryHandler
    : IQueryHandler<GetSharedUserRankingPreviewQuery, ApplicationResult<UserRankingSharePreviewFileResult>>
{
    private const int PreviewItemCount = 5;

    private readonly UserRankingShareAccessResolver accessResolver;
    private readonly IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> parkRankingsHandler;
    private readonly IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> parkItemRankingsHandler;
    private readonly IUserRankingSharePreviewRenderer previewRenderer;

    public GetSharedUserRankingPreviewQueryHandler(
        UserRankingShareAccessResolver accessResolver,
        IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> parkRankingsHandler,
        IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> parkItemRankingsHandler,
        IUserRankingSharePreviewRenderer previewRenderer)
    {
        this.accessResolver = accessResolver;
        this.parkRankingsHandler = parkRankingsHandler;
        this.parkItemRankingsHandler = parkItemRankingsHandler;
        this.previewRenderer = previewRenderer;
    }

    public async Task<ApplicationResult<UserRankingSharePreviewFileResult>> HandleAsync(
        GetSharedUserRankingPreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<UserRankingShareOwner> ownerResult = await this.accessResolver.ResolveAsync(
            query.ShareId,
            cancellationToken);
        if (!ownerResult.IsSuccess || ownerResult.Value is null)
        {
            return ApplicationResult<UserRankingSharePreviewFileResult>.Failure(ownerResult.Errors);
        }

        IReadOnlyCollection<UserRankingSharePreviewItemResult> items = query.ParkItemCategory.HasValue
            ? await this.LoadParkItemPreviewAsync(ownerResult.Value.UserId, query, cancellationToken)
            : await this.LoadParkPreviewAsync(ownerResult.Value.UserId, cancellationToken);
        UserRankingSharePreviewResult preview = new UserRankingSharePreviewResult(
            ownerResult.Value.DisplayName,
            items);
        byte[] content = await this.previewRenderer.RenderPngAsync(preview, cancellationToken);
        return ApplicationResult<UserRankingSharePreviewFileResult>.Success(
            new UserRankingSharePreviewFileResult(content, "image/png"));
    }

    private async Task<IReadOnlyCollection<UserRankingSharePreviewItemResult>> LoadParkPreviewAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        ApplicationResult<PagedResult<UserParkRatingRankingResult>> result = await this.parkRankingsHandler.HandleAsync(
            new GetUserParkRatingRankingsQuery(
                userId,
                new PagedQuery(1, PreviewItemCount),
                PublicTargetsOnly: true),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Array.Empty<UserRankingSharePreviewItemResult>();
        }

        return result.Value.Items.Select(static item => new UserRankingSharePreviewItemResult(
            item.Rank,
            item.ParkName,
            null,
            item.AverageRating)).ToList();
    }

    private async Task<IReadOnlyCollection<UserRankingSharePreviewItemResult>> LoadParkItemPreviewAsync(
        string userId,
        GetSharedUserRankingPreviewQuery query,
        CancellationToken cancellationToken)
    {
        ApplicationResult<PagedResult<UserParkItemRatingRankingResult>> result = await this.parkItemRankingsHandler.HandleAsync(
            new GetUserParkItemRatingRankingsQuery(
                userId,
                query.ParkItemCategory!.Value,
                new PagedQuery(1, PreviewItemCount),
                ParkItemType: query.ParkItemType,
                PublicTargetsOnly: true),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return Array.Empty<UserRankingSharePreviewItemResult>();
        }

        return result.Value.Items.Select(static item => new UserRankingSharePreviewItemResult(
            item.Rank,
            item.Rating.TargetName,
            item.Rating.ParkName,
            item.Rating.Value)).ToList();
    }
}
