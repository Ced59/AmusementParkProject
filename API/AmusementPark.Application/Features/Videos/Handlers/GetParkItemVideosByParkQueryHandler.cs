using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Application.Features.Videos.Queries;
using AmusementPark.Application.Features.Videos.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Handlers;

public sealed class GetParkItemVideosByParkQueryHandler : IQueryHandler<GetParkItemVideosByParkQuery, ApplicationResult<PagedResult<ParkItemVideoResult>>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly IVideoRepository videoRepository;
    private readonly ParkItemReferenceValidator parkItemReferenceValidator;

    public GetParkItemVideosByParkQueryHandler(
        IParkItemRepository parkItemRepository,
        IVideoRepository videoRepository,
        ParkItemReferenceValidator parkItemReferenceValidator)
    {
        this.parkItemRepository = parkItemRepository;
        this.videoRepository = videoRepository;
        this.parkItemReferenceValidator = parkItemReferenceValidator;
    }

    public async Task<ApplicationResult<PagedResult<ParkItemVideoResult>>> HandleAsync(GetParkItemVideosByParkQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<PagedResult<ParkItemVideoResult>>.Failure(ApplicationErrors.Required(nameof(query.ParkId)));
        }

        if (query.Paging.Page <= 0 || query.Paging.PageSize <= 0)
        {
            return ApplicationResult<PagedResult<ParkItemVideoResult>>.Failure(ApplicationErrors.InvalidPagination());
        }

        string parkId = query.ParkId.Trim();
        ApplicationError? parkError = await this.parkItemReferenceValidator.EnsureParkExistsAsync(parkId, cancellationToken);
        if (parkError is not null)
        {
            return ApplicationResult<PagedResult<ParkItemVideoResult>>.Failure(parkError);
        }

        ClosedEntityFilter closedFilter = query.IncludeHidden ? ClosedEntityFilter.All : ClosedEntityFilter.OpenOnly;
        IReadOnlyCollection<ParkItem> parkItems = await this.parkItemRepository.GetByParkIdAsync(
            parkId,
            query.IncludeHidden,
            closedFilter,
            cancellationToken);

        Dictionary<string, ParkItem> itemById = parkItems
            .Where(item => CanUseItem(item, query.IncludeHidden))
            .ToDictionary(static item => item.Id!, static item => item, StringComparer.OrdinalIgnoreCase);

        if (itemById.Count == 0)
        {
            return ApplicationResult<PagedResult<ParkItemVideoResult>>.Success(
                new PagedResult<ParkItemVideoResult>(Array.Empty<ParkItemVideoResult>(), query.Paging.Page, query.Paging.PageSize, 0));
        }

        VideoSearchCriteria criteria = query.Criteria with
        {
            OwnerType = VideoOwnerType.ParkItem,
            OwnerId = null,
            OwnerIds = itemById.Keys.ToList(),
            IsPublished = query.IncludeHidden ? query.Criteria.IsPublished : true,
            SortBy = string.IsNullOrWhiteSpace(query.Criteria.SortBy) ? "published" : query.Criteria.SortBy,
            SortDirection = string.IsNullOrWhiteSpace(query.Criteria.SortDirection) ? "desc" : query.Criteria.SortDirection
        };

        PagedResult<Video> videoPage = await this.videoRepository.GetPageAsync(query.Paging.Page, query.Paging.PageSize, criteria, cancellationToken);
        List<ParkItemVideoResult> results = new List<ParkItemVideoResult>();

        foreach (Video video in videoPage.Items)
        {
            string? ownerId = NormalizeOwnerId(video.OwnerId);
            if (ownerId is null || !itemById.TryGetValue(ownerId, out ParkItem? item))
            {
                continue;
            }

            results.Add(new ParkItemVideoResult(item, video));
        }

        return ApplicationResult<PagedResult<ParkItemVideoResult>>.Success(
            new PagedResult<ParkItemVideoResult>(results, videoPage.Page, videoPage.PageSize, videoPage.TotalItems));
    }

    private static string? NormalizeOwnerId(string? ownerId)
    {
        return string.IsNullOrWhiteSpace(ownerId) ? null : ownerId.Trim();
    }

    private static bool CanUseItem(ParkItem item, bool includeHidden)
    {
        if (string.IsNullOrWhiteSpace(item.Id))
        {
            return false;
        }

        if (includeHidden)
        {
            return true;
        }

        return item.IsVisible && item.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }
}
