using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Commands;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de création d'un attraction manufacturer.
/// </summary>
public sealed class CreateAttractionManufacturerCommandHandler : ICommandHandler<CreateAttractionManufacturerCommand, ApplicationResult<AttractionManufacturer>>
{
    private readonly IAttractionManufacturerRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="CreateAttractionManufacturerCommandHandler"/>.
    /// </summary>
    public CreateAttractionManufacturerCommandHandler(IAttractionManufacturerRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<AttractionManufacturer>> HandleAsync(CreateAttractionManufacturerCommand command, CancellationToken cancellationToken = default)
    {
        if (command.AttractionManufacturer is null)
        {
            return ApplicationResult<AttractionManufacturer>.Failure(ApplicationErrors.Required(nameof(command.AttractionManufacturer)));
        }

        AttractionManufacturer created = await this.repository.CreateAsync(command.AttractionManufacturer, cancellationToken);
        return ApplicationResult<AttractionManufacturer>.Success(created);
    }
}
