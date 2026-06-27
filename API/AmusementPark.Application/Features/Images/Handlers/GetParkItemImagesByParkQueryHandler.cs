using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Images.Handlers;

public sealed class GetParkItemImagesByParkQueryHandler : IQueryHandler<GetParkItemImagesByParkQuery, ApplicationResult<PagedResult<ParkItemImageResult>>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;
    private readonly ParkItemReferenceValidator parkItemReferenceValidator;

    public GetParkItemImagesByParkQueryHandler(
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository,
        ParkItemReferenceValidator parkItemReferenceValidator)
    {
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
        this.parkItemReferenceValidator = parkItemReferenceValidator;
    }

    public async Task<ApplicationResult<PagedResult<ParkItemImageResult>>> HandleAsync(GetParkItemImagesByParkQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<PagedResult<ParkItemImageResult>>.Failure(ApplicationErrors.Required(nameof(query.ParkId)));
        }

        if (query.Paging.Page <= 0 || query.Paging.PageSize <= 0)
        {
            return ApplicationResult<PagedResult<ParkItemImageResult>>.Failure(ApplicationErrors.InvalidPagination());
        }

        string parkId = query.ParkId.Trim();
        ApplicationError? parkError = await this.parkItemReferenceValidator.EnsureParkExistsAsync(parkId, cancellationToken);
        if (parkError is not null)
        {
            return ApplicationResult<PagedResult<ParkItemImageResult>>.Failure(parkError);
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
            return ApplicationResult<PagedResult<ParkItemImageResult>>.Success(
                new PagedResult<ParkItemImageResult>(Array.Empty<ParkItemImageResult>(), query.Paging.Page, query.Paging.PageSize, 0));
        }

        ImageSearchCriteria criteria = new ImageSearchCriteria(
            Category: ImageCategory.ParkItem,
            OwnerType: ImageOwnerType.ParkItem,
            IsPublished: query.IncludeHidden ? null : true,
            HasOwner: true,
            SortBy: "created",
            SortDirection: "desc",
            OwnerIds: itemById.Keys.ToList());

        PagedResult<Image> imagePage = await this.imageRepository.GetPageAsync(query.Paging.Page, query.Paging.PageSize, criteria, cancellationToken);
        List<ParkItemImageResult> results = new List<ParkItemImageResult>();

        foreach (Image image in imagePage.Items)
        {
            string? ownerId = NormalizeOwnerId(image.OwnerId);
            if (ownerId is null || !itemById.TryGetValue(ownerId, out ParkItem? item))
            {
                continue;
            }

            results.Add(new ParkItemImageResult(item, image));
        }

        return ApplicationResult<PagedResult<ParkItemImageResult>>.Success(
            new PagedResult<ParkItemImageResult>(results, imagePage.Page, imagePage.PageSize, imagePage.TotalItems));
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
