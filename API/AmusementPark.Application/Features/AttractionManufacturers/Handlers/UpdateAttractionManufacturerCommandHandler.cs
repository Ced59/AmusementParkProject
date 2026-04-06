using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Commands;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de mise à jour d'un attraction manufacturer.
/// </summary>
public sealed class UpdateAttractionManufacturerCommandHandler : ICommandHandler<UpdateAttractionManufacturerCommand, ApplicationResult<AttractionManufacturer>>
{
    private readonly IAttractionManufacturerRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="UpdateAttractionManufacturerCommandHandler"/>.
    /// </summary>
    public UpdateAttractionManufacturerCommandHandler(IAttractionManufacturerRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<AttractionManufacturer>> HandleAsync(UpdateAttractionManufacturerCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            return ApplicationResult<AttractionManufacturer>.Failure(ApplicationErrors.Required(nameof(command.Id)));
        }

        if (command.AttractionManufacturer is null)
        {
            return ApplicationResult<AttractionManufacturer>.Failure(ApplicationErrors.Required(nameof(command.AttractionManufacturer)));
        }

        AttractionManufacturer? updated = await this.repository.UpdateAsync(command.Id, command.AttractionManufacturer, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<AttractionManufacturer>.Failure(ApplicationErrors.EntityNotFound("AttractionManufacturer", command.Id));
        }

        return ApplicationResult<AttractionManufacturer>.Success(updated);
    }
}
