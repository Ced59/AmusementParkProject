using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class GetParkItemsByParkIdQueryHandler : IQueryHandler<GetParkItemsByParkIdQuery, ApplicationResult<PagedResult<ParkItemListResult>>>
{
    private const int ManufacturerSearchLimit = 50;

    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly ParkItemReferenceValidator parkItemReferenceValidator;

    public GetParkItemsByParkIdQueryHandler(
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        ParkItemReferenceValidator parkItemReferenceValidator)
    {
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.parkItemReferenceValidator = parkItemReferenceValidator;
    }

    public async Task<ApplicationResult<PagedResult<ParkItemListResult>>> HandleAsync(GetParkItemsByParkIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<PagedResult<ParkItemListResult>>.Failure(ApplicationErrors.Required(nameof(query.ParkId)));
        }

        if (query.Paging.Page <= 0 || query.Paging.PageSize <= 0)
        {
            return ApplicationResult<PagedResult<ParkItemListResult>>.Failure(ApplicationErrors.InvalidPagination());
        }

        ApplicationError? parkError = await this.parkItemReferenceValidator.EnsureParkExistsAsync(query.ParkId, cancellationToken);
        if (parkError is not null)
        {
            return ApplicationResult<PagedResult<ParkItemListResult>>.Failure(parkError);
        }

        IReadOnlyCollection<string> manufacturerIds = await this.ResolveManufacturerIdsAsync(query.Search, query.IncludeHidden, cancellationToken);

        PagedResult<ParkItem> page = await this.parkItemRepository.GetPublicPageByParkIdAsync(
            query.Paging.Page,
            query.Paging.PageSize,
            query.ParkId.Trim(),
            query.Search,
            query.IncludeHidden,
            query.ClosedFilter,
            query.Category,
            query.Type,
            query.ZoneId,
            manufacturerIds,
            cancellationToken);

        IReadOnlyCollection<string> itemIds = page.Items
            .Select(static item => item.Id)
            .Where(static itemId => !string.IsNullOrWhiteSpace(itemId))
            .Select(static itemId => itemId!)
            .ToList();

        IReadOnlyDictionary<string, string> mainImageIds = await this.imageRepository.GetMainImageIdsByOwnersAsync(
            ImageOwnerType.ParkItem,
            itemIds,
            ImageCategory.ParkItem,
            !query.IncludeHidden,
            cancellationToken);

        List<ParkItemListResult> results = page.Items
            .Select(item => new ParkItemListResult
            {
                Item = item,
                MainImageId = !string.IsNullOrWhiteSpace(item.Id) && mainImageIds.TryGetValue(item.Id, out string? imageId) ? imageId : null,
            })
            .ToList();

        return ApplicationResult<PagedResult<ParkItemListResult>>.Success(
            new PagedResult<ParkItemListResult>(results, page.Page, page.PageSize, page.TotalItems));
    }

    private async Task<IReadOnlyCollection<string>> ResolveManufacturerIdsAsync(string? search, bool includeHidden, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return Array.Empty<string>();
        }

        return await this.attractionManufacturerRepository.SearchIdsAsync(
            search.Trim(),
            includeHidden,
            ManufacturerSearchLimit,
            cancellationToken);
    }
}
