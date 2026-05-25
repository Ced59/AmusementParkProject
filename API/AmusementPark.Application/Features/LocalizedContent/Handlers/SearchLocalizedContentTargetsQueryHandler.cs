using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.LocalizedContent.Queries;
using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;

/// <summary>
/// Handler de recherche de cibles localisables administrables.
/// </summary>
public sealed class SearchLocalizedContentTargetsQueryHandler : IQueryHandler<SearchLocalizedContentTargetsQuery, ApplicationResult<PagedResult<LocalizedContentTargetResult>>>
{
    private const int MaximumPageSize = 50;
    private readonly IParkRepository parkRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOperatorRepository parkOperatorRepository;
    private readonly IParkFounderRepository parkFounderRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;
    private readonly IImageRepository imageRepository;
    private readonly IImageTagRepository imageTagRepository;

    public SearchLocalizedContentTargetsQueryHandler(
        IParkRepository parkRepository,
        IParkZoneRepository parkZoneRepository,
        IParkItemRepository parkItemRepository,
        IParkOperatorRepository parkOperatorRepository,
        IParkFounderRepository parkFounderRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository,
        IImageRepository imageRepository,
        IImageTagRepository imageTagRepository)
    {
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkOperatorRepository = parkOperatorRepository;
        this.parkFounderRepository = parkFounderRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
        this.imageRepository = imageRepository;
        this.imageTagRepository = imageTagRepository;
    }

    public async Task<ApplicationResult<PagedResult<LocalizedContentTargetResult>>> HandleAsync(SearchLocalizedContentTargetsQuery query, CancellationToken cancellationToken = default)
    {
        if (!LocalizedContentEntityTypeParser.TryParse(query.EntityType, out LocalizedContentEntityType entityType))
        {
            return ApplicationResult<PagedResult<LocalizedContentTargetResult>>.Failure(LocalizedContentApplicationErrors.InvalidEntityType(query.EntityType));
        }

        int page = Math.Max(1, query.Page);
        int pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        string search = query.Search?.Trim() ?? string.Empty;

        PagedResult<LocalizedContentTargetResult> result = entityType switch
        {
            LocalizedContentEntityType.Park => await this.SearchParksAsync(search, page, pageSize, cancellationToken),
            LocalizedContentEntityType.ParkItem => await this.SearchParkItemsAsync(search, page, pageSize, cancellationToken),
            LocalizedContentEntityType.ParkZone => await this.SearchParkZonesAsync(search, page, pageSize, cancellationToken),
            LocalizedContentEntityType.ParkOperator => await this.SearchParkOperatorsAsync(search, page, pageSize, cancellationToken),
            LocalizedContentEntityType.ParkFounder => await this.SearchParkFoundersAsync(search, page, pageSize, cancellationToken),
            LocalizedContentEntityType.AttractionManufacturer => await this.SearchAttractionManufacturersAsync(search, page, pageSize, cancellationToken),
            LocalizedContentEntityType.Image => await this.SearchImagesAsync(search, page, pageSize, cancellationToken),
            LocalizedContentEntityType.ImageTag => await this.SearchImageTagsAsync(search, page, pageSize, cancellationToken),
            _ => new PagedResult<LocalizedContentTargetResult>(Array.Empty<LocalizedContentTargetResult>(), page, pageSize, 0),
        };

        return ApplicationResult<PagedResult<LocalizedContentTargetResult>>.Success(result);
    }

    private async Task<PagedResult<LocalizedContentTargetResult>> SearchParksAsync(string search, int page, int pageSize, CancellationToken cancellationToken)
    {
        PagedResult<Park> parks = string.IsNullOrWhiteSpace(search)
            ? await this.parkRepository.GetPageAsync(page, pageSize, true, null, null, null, null, cancellationToken)
            : await this.parkRepository.SearchAsync(new ParkSearchCriteria(search, Array.Empty<string>(), Array.Empty<string>()), page, pageSize, true, null, null, null, null, cancellationToken);

        return parks.Map(static park => new LocalizedContentTargetResult(
            LocalizedContentEntityTypes.Park,
            park.Id,
            park.Name ?? park.Id,
            BuildContext(park.City, park.CountryCode),
            LocalizedContentSupportedFields.For(LocalizedContentEntityType.Park)));
    }

    private async Task<PagedResult<LocalizedContentTargetResult>> SearchParkItemsAsync(string search, int page, int pageSize, CancellationToken cancellationToken)
    {
        PagedResult<ParkItem> items = await this.parkItemRepository.GetPageAsync(
            page,
            pageSize,
            null,
            search,
            true,
            null,
            null,
            null,
            null,
            null,
            cancellationToken);

        return items.Map(static item => new LocalizedContentTargetResult(
            LocalizedContentEntityTypes.ParkItem,
            item.Id,
            item.Name,
            $"Parc {item.ParkId} · {item.Category} · {item.Type}",
            LocalizedContentSupportedFields.For(LocalizedContentEntityType.ParkItem)));
    }

    private async Task<PagedResult<LocalizedContentTargetResult>> SearchParkZonesAsync(string search, int page, int pageSize, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkZone> zones = await this.parkZoneRepository.GetAllAsync(cancellationToken);
        List<LocalizedContentTargetResult> targets = zones
            .Where(zone => Matches(search, zone.Id, zone.Name, zone.ParkId, LocalizedValues(zone.Names), LocalizedValues(zone.Descriptions)))
            .OrderBy(static zone => zone.ParkId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static zone => zone.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static zone => new LocalizedContentTargetResult(
                LocalizedContentEntityTypes.ParkZone,
                zone.Id,
                string.IsNullOrWhiteSpace(zone.Name) ? zone.Id : zone.Name,
                $"Parc {zone.ParkId}",
                LocalizedContentSupportedFields.For(LocalizedContentEntityType.ParkZone)))
            .ToList();

        return ToPagedResult(targets, page, pageSize);
    }

    private async Task<PagedResult<LocalizedContentTargetResult>> SearchParkOperatorsAsync(string search, int page, int pageSize, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkOperator> operators = await this.parkOperatorRepository.GetAllAsync(cancellationToken);
        List<LocalizedContentTargetResult> targets = operators
            .Where(value => Matches(search, value.Id, value.Name, value.LegalName, LocalizedValues(value.Description)))
            .OrderBy(static value => value.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static value => new LocalizedContentTargetResult(
                LocalizedContentEntityTypes.ParkOperator,
                value.Id,
                value.Name,
                value.LegalName,
                LocalizedContentSupportedFields.For(LocalizedContentEntityType.ParkOperator)))
            .ToList();

        return ToPagedResult(targets, page, pageSize);
    }

    private async Task<PagedResult<LocalizedContentTargetResult>> SearchParkFoundersAsync(string search, int page, int pageSize, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkFounder> founders = await this.parkFounderRepository.GetAllAsync(cancellationToken);
        List<LocalizedContentTargetResult> targets = founders
            .Where(value => Matches(search, value.Id, value.Name, value.Occupation, value.BirthPlace, LocalizedValues(value.Biography)))
            .OrderBy(static value => value.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static value => new LocalizedContentTargetResult(
                LocalizedContentEntityTypes.ParkFounder,
                value.Id,
                value.Name,
                value.Occupation,
                LocalizedContentSupportedFields.For(LocalizedContentEntityType.ParkFounder)))
            .ToList();

        return ToPagedResult(targets, page, pageSize);
    }

    private async Task<PagedResult<LocalizedContentTargetResult>> SearchAttractionManufacturersAsync(string search, int page, int pageSize, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AttractionManufacturer> manufacturers = await this.attractionManufacturerRepository.GetAllAsync(cancellationToken);
        List<LocalizedContentTargetResult> targets = manufacturers
            .Where(value => Matches(search, value.Id, value.Name, value.LegalName, LocalizedValues(value.Biography)))
            .OrderBy(static value => value.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static value => new LocalizedContentTargetResult(
                LocalizedContentEntityTypes.AttractionManufacturer,
                value.Id,
                value.Name,
                value.LegalName,
                LocalizedContentSupportedFields.For(LocalizedContentEntityType.AttractionManufacturer)))
            .ToList();

        return ToPagedResult(targets, page, pageSize);
    }

    private async Task<PagedResult<LocalizedContentTargetResult>> SearchImagesAsync(string search, int page, int pageSize, CancellationToken cancellationToken)
    {
        PagedResult<Image> images = await this.imageRepository.GetPageAsync(
            page,
            pageSize,
            new ImageSearchCriteria(search, null, null, null, null, null),
            cancellationToken);

        return images.Map(static image => new LocalizedContentTargetResult(
            LocalizedContentEntityTypes.Image,
            image.Id,
            string.IsNullOrWhiteSpace(image.OriginalFileName) ? image.Id : image.OriginalFileName,
            $"{image.Category} · {image.OwnerType} {image.OwnerId}",
            LocalizedContentSupportedFields.For(LocalizedContentEntityType.Image)));
    }

    private async Task<PagedResult<LocalizedContentTargetResult>> SearchImageTagsAsync(string search, int page, int pageSize, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ImageTag> tags = await this.imageTagRepository.GetAllAsync(cancellationToken);
        List<LocalizedContentTargetResult> targets = tags
            .Where(value => Matches(search, value.Id, value.Slug, LocalizedValues(value.Labels), LocalizedValues(value.Descriptions)))
            .OrderBy(static value => value.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(static value => new LocalizedContentTargetResult(
                LocalizedContentEntityTypes.ImageTag,
                value.Id,
                value.Slug,
                value.IsActive ? "Actif" : "Inactif",
                LocalizedContentSupportedFields.For(LocalizedContentEntityType.ImageTag)))
            .ToList();

        return ToPagedResult(targets, page, pageSize);
    }

    private static PagedResult<LocalizedContentTargetResult> ToPagedResult(IReadOnlyCollection<LocalizedContentTargetResult> values, int page, int pageSize)
    {
        List<LocalizedContentTargetResult> items = values
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<LocalizedContentTargetResult>(items, page, pageSize, values.Count);
    }

    private static bool Matches(string search, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return values.Any(value => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string LocalizedValues(IEnumerable<LocalizedText>? values)
    {
        return values is null ? string.Empty : string.Join(" ", values.Select(static value => value.Value));
    }

    private static string? BuildContext(params string?[] values)
    {
        string context = string.Join(" · ", values.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(static value => value!.Trim()));
        return string.IsNullOrWhiteSpace(context) ? null : context;
    }
}

internal static class LocalizedContentPagedResultExtensions
{
    public static PagedResult<TTarget> Map<TSource, TTarget>(this PagedResult<TSource> source, Func<TSource, TTarget> mapper)
    {
        return new PagedResult<TTarget>(source.Items.Select(mapper).ToList(), source.Page, source.PageSize, source.TotalItems);
    }
}
