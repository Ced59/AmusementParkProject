using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetUserRankingShareSettingsQueryHandler
    : IQueryHandler<GetUserRankingShareSettingsQuery, ApplicationResult<UserRankingShareSettingsResult>>
{
    private readonly IUserRankingShareRepository shareRepository;

    public GetUserRankingShareSettingsQueryHandler(IUserRankingShareRepository shareRepository)
    {
        this.shareRepository = shareRepository;
    }

    public async Task<ApplicationResult<UserRankingShareSettingsResult>> HandleAsync(
        GetUserRankingShareSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<UserRankingShareSettingsResult>.Failure(
                ApplicationErrors.Required(nameof(query.UserId)));
        }

        UserRankingShare? share = await this.shareRepository.GetByUserIdAsync(
            query.UserId.Trim(),
            cancellationToken);
        return ApplicationResult<UserRankingShareSettingsResult>.Success(ToSettings(share));
    }

    internal static UserRankingShareSettingsResult ToSettings(UserRankingShare? share)
    {
        return new UserRankingShareSettingsResult(
            share?.IsPublic == true,
            share?.IsPublic == true ? share.ShareId : null,
            share?.IsPublic == true ? share.PublishedAtUtc : null);
    }
}

public sealed class SetUserRankingShareVisibilityCommandHandler
    : ICommandHandler<SetUserRankingShareVisibilityCommand, ApplicationResult<UserRankingShareSettingsResult>>
{
    private readonly IUserRankingShareRepository shareRepository;
    private readonly IUserRankingShareIdFactory shareIdFactory;
    private readonly TimeProvider timeProvider;

    public SetUserRankingShareVisibilityCommandHandler(
        IUserRankingShareRepository shareRepository,
        IUserRankingShareIdFactory shareIdFactory,
        TimeProvider? timeProvider = null)
    {
        this.shareRepository = shareRepository;
        this.shareIdFactory = shareIdFactory;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<UserRankingShareSettingsResult>> HandleAsync(
        SetUserRankingShareVisibilityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return ApplicationResult<UserRankingShareSettingsResult>.Failure(
                ApplicationErrors.Required(nameof(command.UserId)));
        }

        string userId = command.UserId.Trim();
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        UserRankingShare? share = await this.shareRepository.GetByUserIdAsync(userId, cancellationToken);
        if (share is null)
        {
            if (!command.IsPublic)
            {
                return ApplicationResult<UserRankingShareSettingsResult>.Success(
                    GetUserRankingShareSettingsQueryHandler.ToSettings(null));
            }

            share = UserRankingShare.Create(userId, nowUtc);
        }

        if (command.IsPublic)
        {
            share.Publish(this.shareIdFactory.Generate(), nowUtc);
        }
        else
        {
            share.Revoke(nowUtc);
        }

        UserRankingShare savedShare = await this.shareRepository.UpsertAsync(share, cancellationToken);
        return ApplicationResult<UserRankingShareSettingsResult>.Success(
            GetUserRankingShareSettingsQueryHandler.ToSettings(savedShare));
    }
}

public sealed class GetSharedUserRankingProfileQueryHandler
    : IQueryHandler<GetSharedUserRankingProfileQuery, ApplicationResult<SharedUserRankingProfileResult>>
{
    private readonly UserRankingShareAccessResolver accessResolver;
    private readonly IRatingRepository ratingRepository;

    public GetSharedUserRankingProfileQueryHandler(
        UserRankingShareAccessResolver accessResolver,
        IRatingRepository ratingRepository)
    {
        this.accessResolver = accessResolver;
        this.ratingRepository = ratingRepository;
    }

    public async Task<ApplicationResult<SharedUserRankingProfileResult>> HandleAsync(
        GetSharedUserRankingProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<UserRankingShareOwner> ownerResult = await this.accessResolver.ResolveAsync(
            query.ShareId,
            cancellationToken);
        if (!ownerResult.IsSuccess || ownerResult.Value is null)
        {
            return ApplicationResult<SharedUserRankingProfileResult>.Failure(ownerResult.Errors);
        }

        UserRatingStatsResult stats = await this.ratingRepository.GetVisibleUserRatingStatsAsync(
            ownerResult.Value.UserId,
            cancellationToken);
        return ApplicationResult<SharedUserRankingProfileResult>.Success(
            new SharedUserRankingProfileResult(
                ownerResult.Value.UserId,
                ownerResult.Value.DisplayName,
                ownerResult.Value.PublishedAtUtc,
                stats));
    }
}

public sealed class GetSharedUserParkRatingRankingsQueryHandler
    : IQueryHandler<GetSharedUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>>
{
    private readonly UserRankingShareAccessResolver accessResolver;
    private readonly IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> rankingsHandler;

    public GetSharedUserParkRatingRankingsQueryHandler(
        UserRankingShareAccessResolver accessResolver,
        IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> rankingsHandler)
    {
        this.accessResolver = accessResolver;
        this.rankingsHandler = rankingsHandler;
    }

    public async Task<ApplicationResult<PagedResult<UserParkRatingRankingResult>>> HandleAsync(
        GetSharedUserParkRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<UserRankingShareOwner> ownerResult = await this.accessResolver.ResolveAsync(
            query.ShareId,
            cancellationToken);
        if (!ownerResult.IsSuccess || ownerResult.Value is null)
        {
            return ApplicationResult<PagedResult<UserParkRatingRankingResult>>.Failure(ownerResult.Errors);
        }

        return await this.rankingsHandler.HandleAsync(
            new GetUserParkRatingRankingsQuery(
                ownerResult.Value.UserId,
                query.Paging,
                query.ParkSearch,
                PublicTargetsOnly: true),
            cancellationToken);
    }
}

public sealed class GetSharedUserParkItemRatingRankingsQueryHandler
    : IQueryHandler<GetSharedUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>
{
    private readonly UserRankingShareAccessResolver accessResolver;
    private readonly IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> rankingsHandler;

    public GetSharedUserParkItemRatingRankingsQueryHandler(
        UserRankingShareAccessResolver accessResolver,
        IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> rankingsHandler)
    {
        this.accessResolver = accessResolver;
        this.rankingsHandler = rankingsHandler;
    }

    public async Task<ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> HandleAsync(
        GetSharedUserParkItemRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<UserRankingShareOwner> ownerResult = await this.accessResolver.ResolveAsync(
            query.ShareId,
            cancellationToken);
        if (!ownerResult.IsSuccess || ownerResult.Value is null)
        {
            return ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>.Failure(ownerResult.Errors);
        }

        return await this.rankingsHandler.HandleAsync(
            new GetUserParkItemRatingRankingsQuery(
                ownerResult.Value.UserId,
                query.ParkItemCategory,
                query.Paging,
                query.Search,
                query.ParkItemType,
                PublicTargetsOnly: true),
            cancellationToken);
    }
}

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
