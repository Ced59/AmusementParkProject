using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de récupération de la liste des attraction manufacturers.
/// </summary>
public sealed class GetAttractionManufacturersQueryHandler : IQueryHandler<GetAttractionManufacturersQuery, ApplicationResult<IReadOnlyCollection<AttractionManufacturer>>>
{
    private readonly IAttractionManufacturerRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetAttractionManufacturersQueryHandler"/>.
    /// </summary>
    public GetAttractionManufacturersQueryHandler(IAttractionManufacturerRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<IReadOnlyCollection<AttractionManufacturer>>> HandleAsync(GetAttractionManufacturersQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<AttractionManufacturer> entities = await this.repository.GetAllAsync(cancellationToken);
        return ApplicationResult<IReadOnlyCollection<AttractionManufacturer>>.Success(entities);
    }
}
