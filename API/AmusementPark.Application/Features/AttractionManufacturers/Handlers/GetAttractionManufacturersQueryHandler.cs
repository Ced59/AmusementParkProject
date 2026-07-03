using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Queries;
using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de récupération de la liste des attraction manufacturers.
/// </summary>
public sealed class GetAttractionManufacturersQueryHandler : IQueryHandler<GetAttractionManufacturersQuery, ApplicationResult<PagedResult<AttractionManufacturerResult>>>
{
    private readonly IAttractionManufacturerRepository repository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetAttractionManufacturersQueryHandler"/>.
    /// </summary>
    public GetAttractionManufacturersQueryHandler(
        IAttractionManufacturerRepository repository,
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository,
        PagedQueryValidator pagedQueryValidator)
    {
        this.repository = repository;
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<PagedResult<AttractionManufacturerResult>>> HandleAsync(GetAttractionManufacturersQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<AttractionManufacturerResult>>.Failure(errors);
        }

        PagedResult<AttractionManufacturer> page = await this.repository.GetPageAsync(
            query.Paging.Page,
            query.Paging.PageSize,
            query.Search,
            query.IncludeHidden,
            cancellationToken);

        List<string> ids = page.Items.Where(static entity => !string.IsNullOrWhiteSpace(entity.Id)).Select(static entity => entity.Id).Cast<string>().ToList();
        IReadOnlyDictionary<string, int> counts = await this.parkItemRepository.GetAttractionCountsByManufacturerIdsAsync(ids, cancellationToken, includeHidden: false);
        IReadOnlyCollection<Image> logoImages = await this.imageRepository.GetByOwnersAsync(ImageOwnerType.AttractionManufacturer, ids, ImageCategory.Logo, cancellationToken);
        Dictionary<string, HashSet<string>> publishedLogoImageIdsByOwner = AttractionManufacturerImageSelection.BuildPublishedLogoImageIdsByOwner(logoImages);
        IReadOnlyDictionary<string, string> logoImageIds = await this.imageRepository.GetMainImageIdsByOwnersAsync(ImageOwnerType.AttractionManufacturer, ids, ImageCategory.Logo, publishedOnly: true, cancellationToken);
        IReadOnlyDictionary<string, string> manufacturerImageIds = await this.imageRepository.GetMainImageIdsByOwnersAsync(ImageOwnerType.AttractionManufacturer, ids, ImageCategory.Manufacturer, publishedOnly: true, cancellationToken);

        IReadOnlyCollection<AttractionManufacturerResult> results = page.Items.Select(entity => new AttractionManufacturerResult
        {
            Id = entity.Id,
            Name = entity.Name,
            LegalName = entity.LegalName,
            FoundedYear = entity.FoundedYear,
            ClosedYear = entity.ClosedYear,
            ContactDetails = entity.ContactDetails,
            Biography = entity.Biography,
            CurrentLogoImageId = AttractionManufacturerImageSelection.ResolveLogoImageId(entity, logoImageIds, publishedLogoImageIdsByOwner),
            MainImageId = AttractionManufacturerImageSelection.ResolveMainImageId(entity, logoImageIds, manufacturerImageIds, publishedLogoImageIdsByOwner),
            IsVisible = entity.IsVisible,
            AdminReviewStatus = entity.AdminReviewStatus,
            AttractionCount = !string.IsNullOrWhiteSpace(entity.Id) && counts.TryGetValue(entity.Id, out int value) ? value : 0,
        }).ToList();

        PagedResult<AttractionManufacturerResult> result = new PagedResult<AttractionManufacturerResult>(
            results,
            page.Page,
            page.PageSize,
            page.TotalItems);

        return ApplicationResult<PagedResult<AttractionManufacturerResult>>.Success(result);
    }
}
