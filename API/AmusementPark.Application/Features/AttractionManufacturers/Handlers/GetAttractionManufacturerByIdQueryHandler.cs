using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Queries;
using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Application.Features.ParkItems.Ports;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de récupération d'un attraction manufacturer par identifiant.
/// </summary>
public sealed class GetAttractionManufacturerByIdQueryHandler : IQueryHandler<GetAttractionManufacturerByIdQuery, ApplicationResult<AttractionManufacturerResult>>
{
    private readonly IAttractionManufacturerRepository repository;
    private readonly IParkItemRepository parkItemRepository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetAttractionManufacturerByIdQueryHandler"/>.
    /// </summary>
    public GetAttractionManufacturerByIdQueryHandler(IAttractionManufacturerRepository repository, IParkItemRepository parkItemRepository)
    {
        this.repository = repository;
        this.parkItemRepository = parkItemRepository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<AttractionManufacturerResult>> HandleAsync(GetAttractionManufacturerByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Id))
        {
            return ApplicationResult<AttractionManufacturerResult>.Failure(ApplicationError.NotFound("attraction-manufacturer.not-found", "Attraction manufacturer not exists"));
        }

        AmusementPark.Core.Domain.Parks.AttractionManufacturer? entity = await this.repository.GetByIdAsync(query.Id, cancellationToken);
        if (entity is null)
        {
            return ApplicationResult<AttractionManufacturerResult>.Failure(ApplicationError.NotFound("attraction-manufacturer.not-found", "Attraction manufacturer not exists"));
        }

        IReadOnlyDictionary<string, int> counts = await this.parkItemRepository.GetAttractionCountsByManufacturerIdsAsync(new[] { query.Id }, cancellationToken);
        int attractionCount = counts.TryGetValue(query.Id, out int value) ? value : 0;

        return ApplicationResult<AttractionManufacturerResult>.Success(new AttractionManufacturerResult
        {
            Id = entity.Id,
            Name = entity.Name,
            Biography = entity.Biography,
            AdminReviewStatus = entity.AdminReviewStatus,
            AttractionCount = attractionCount,
        });
    }
}
