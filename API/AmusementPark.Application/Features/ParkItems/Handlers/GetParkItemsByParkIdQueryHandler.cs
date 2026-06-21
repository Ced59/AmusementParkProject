using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class GetParkItemsByParkIdQueryHandler : IQueryHandler<GetParkItemsByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkItemListResult>>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;
    private readonly ParkItemReferenceValidator parkItemReferenceValidator;

    public GetParkItemsByParkIdQueryHandler(
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository,
        ParkItemReferenceValidator parkItemReferenceValidator)
    {
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
        this.parkItemReferenceValidator = parkItemReferenceValidator;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ParkItemListResult>>> HandleAsync(GetParkItemsByParkIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<IReadOnlyCollection<ParkItemListResult>>.Failure(ApplicationErrors.Required(nameof(query.ParkId)));
        }

        ApplicationError? parkError = await this.parkItemReferenceValidator.EnsureParkExistsAsync(query.ParkId, cancellationToken);
        if (parkError is not null)
        {
            return ApplicationResult<IReadOnlyCollection<ParkItemListResult>>.Failure(parkError);
        }

        IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByParkIdAsync(query.ParkId.Trim(), query.IncludeHidden, query.ClosedFilter, cancellationToken);
        IReadOnlyCollection<string> itemIds = items
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

        List<ParkItemListResult> results = items
            .Select(item => new ParkItemListResult
            {
                Item = item,
                MainImageId = !string.IsNullOrWhiteSpace(item.Id) && mainImageIds.TryGetValue(item.Id, out string? imageId) ? imageId : null,
            })
            .ToList();

        return ApplicationResult<IReadOnlyCollection<ParkItemListResult>>.Success(results);
    }
}
