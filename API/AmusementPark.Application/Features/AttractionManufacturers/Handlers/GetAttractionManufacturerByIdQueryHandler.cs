using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Queries;
using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de récupération d'un attraction manufacturer par identifiant.
/// </summary>
public sealed class GetAttractionManufacturerByIdQueryHandler : IQueryHandler<GetAttractionManufacturerByIdQuery, ApplicationResult<AttractionManufacturerResult>>
{
    private readonly IAttractionManufacturerRepository repository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetAttractionManufacturerByIdQueryHandler"/>.
    /// </summary>
    public GetAttractionManufacturerByIdQueryHandler(IAttractionManufacturerRepository repository, IParkItemRepository parkItemRepository, IImageRepository imageRepository)
    {
        this.repository = repository;
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<AttractionManufacturerResult>> HandleAsync(GetAttractionManufacturerByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Id))
        {
            return ApplicationResult<AttractionManufacturerResult>.Failure(ApplicationError.NotFound("attraction-manufacturer.not-found", "Attraction manufacturer not exists"));
        }

        AmusementPark.Core.Domain.Parks.AttractionManufacturer? entity = await this.repository.GetByIdAsync(query.Id, cancellationToken);
        if (entity is null || (!query.IncludeHidden && !entity.IsVisible))
        {
            return ApplicationResult<AttractionManufacturerResult>.Failure(ApplicationError.NotFound("attraction-manufacturer.not-found", "Attraction manufacturer not exists"));
        }

        IReadOnlyDictionary<string, int> counts = await this.parkItemRepository.GetAttractionCountsByManufacturerIdsAsync(new[] { query.Id }, cancellationToken, includeHidden: false);
        int attractionCount = counts.TryGetValue(query.Id, out int value) ? value : 0;
        IReadOnlyDictionary<string, string> logoImageIds = await this.imageRepository.GetMainImageIdsByOwnersAsync(ImageOwnerType.AttractionManufacturer, new[] { query.Id }, ImageCategory.Logo, publishedOnly: true, cancellationToken);
        IReadOnlyDictionary<string, string> manufacturerImageIds = await this.imageRepository.GetMainImageIdsByOwnersAsync(ImageOwnerType.AttractionManufacturer, new[] { query.Id }, ImageCategory.Manufacturer, publishedOnly: true, cancellationToken);
        string? currentLogoImageId = logoImageIds.GetValueOrDefault(query.Id);
        string? mainImageId = !string.IsNullOrWhiteSpace(currentLogoImageId)
            ? currentLogoImageId
            : manufacturerImageIds.GetValueOrDefault(query.Id);

        return ApplicationResult<AttractionManufacturerResult>.Success(new AttractionManufacturerResult
        {
            Id = entity.Id,
            Name = entity.Name,
            LegalName = entity.LegalName,
            FoundedYear = entity.FoundedYear,
            ClosedYear = entity.ClosedYear,
            ContactDetails = entity.ContactDetails,
            Biography = entity.Biography,
            CurrentLogoImageId = currentLogoImageId,
            MainImageId = mainImageId,
            IsVisible = entity.IsVisible,
            AdminReviewStatus = entity.AdminReviewStatus,
            AttractionCount = attractionCount,
        });
    }
}
