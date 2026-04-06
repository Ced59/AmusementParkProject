using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Queries;
using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de récupération de la liste des attraction manufacturers.
/// </summary>
public sealed class GetAttractionManufacturersQueryHandler : IQueryHandler<GetAttractionManufacturersQuery, ApplicationResult<IReadOnlyCollection<AttractionManufacturerResult>>>
{
    private readonly IAttractionManufacturerRepository repository;
    private readonly IParkItemRepository parkItemRepository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetAttractionManufacturersQueryHandler"/>.
    /// </summary>
    public GetAttractionManufacturersQueryHandler(IAttractionManufacturerRepository repository, IParkItemRepository parkItemRepository)
    {
        this.repository = repository;
        this.parkItemRepository = parkItemRepository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<IReadOnlyCollection<AttractionManufacturerResult>>> HandleAsync(GetAttractionManufacturersQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<AttractionManufacturer> entities = await this.repository.GetAllAsync(cancellationToken);
        List<string> ids = entities.Where(static entity => !string.IsNullOrWhiteSpace(entity.Id)).Select(static entity => entity.Id).Cast<string>().ToList();
        IReadOnlyDictionary<string, int> counts = await this.parkItemRepository.GetAttractionCountsByManufacturerIdsAsync(ids, cancellationToken);

        IReadOnlyCollection<AttractionManufacturerResult> results = entities.Select(entity => new AttractionManufacturerResult
        {
            Id = entity.Id,
            Name = entity.Name,
            Biography = entity.Biography,
            AttractionCount = !string.IsNullOrWhiteSpace(entity.Id) && counts.TryGetValue(entity.Id, out int value) ? value : 0,
        }).ToList();

        return ApplicationResult<IReadOnlyCollection<AttractionManufacturerResult>>.Success(results);
    }
}
