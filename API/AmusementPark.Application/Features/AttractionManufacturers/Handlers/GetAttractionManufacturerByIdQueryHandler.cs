using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de récupération d'un attraction manufacturer par identifiant.
/// </summary>
public sealed class GetAttractionManufacturerByIdQueryHandler : IQueryHandler<GetAttractionManufacturerByIdQuery, ApplicationResult<AttractionManufacturer>>
{
    private readonly IAttractionManufacturerRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetAttractionManufacturerByIdQueryHandler"/>.
    /// </summary>
    public GetAttractionManufacturerByIdQueryHandler(IAttractionManufacturerRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<AttractionManufacturer>> HandleAsync(GetAttractionManufacturerByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Id))
        {
            return ApplicationResult<AttractionManufacturer>.Failure(ApplicationErrors.Required(nameof(query.Id)));
        }

        AttractionManufacturer? entity = await this.repository.GetByIdAsync(query.Id, cancellationToken);
        if (entity is null)
        {
            return ApplicationResult<AttractionManufacturer>.Failure(ApplicationErrors.EntityNotFound("AttractionManufacturer", query.Id));
        }

        return ApplicationResult<AttractionManufacturer>.Success(entity);
    }
}
