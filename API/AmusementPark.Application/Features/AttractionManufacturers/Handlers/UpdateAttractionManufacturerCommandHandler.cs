using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Commands;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de mise à jour d'un attraction manufacturer.
/// </summary>
public sealed class UpdateAttractionManufacturerCommandHandler : ICommandHandler<UpdateAttractionManufacturerCommand, ApplicationResult<AttractionManufacturerResult>>
{
    private readonly IAttractionManufacturerRepository repository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="UpdateAttractionManufacturerCommandHandler"/>.
    /// </summary>
    public UpdateAttractionManufacturerCommandHandler(IAttractionManufacturerRepository repository, IParkItemRepository parkItemRepository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<AttractionManufacturerResult>> HandleAsync(UpdateAttractionManufacturerCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            return ApplicationResult<AttractionManufacturerResult>.Failure(ApplicationError.NotFound("attraction-manufacturer.not-found", "Attraction manufacturer not exists"));
        }

        if (command.AttractionManufacturer is null)
        {
            return ApplicationResult<AttractionManufacturerResult>.Failure(ApplicationErrors.Required(nameof(command.AttractionManufacturer)));
        }

        try
        {
            AttractionManufacturer? updated = await this.repository.UpdateAsync(command.Id, command.AttractionManufacturer, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<AttractionManufacturerResult>.Failure(ApplicationError.NotFound("attraction-manufacturer.not-found", "Attraction manufacturer not exists"));
            }

            await this.searchProjectionWriter.UpsertAsync("manufacturers", updated.Id, cancellationToken);
            IReadOnlyDictionary<string, int> counts = await this.parkItemRepository.GetAttractionCountsByManufacturerIdsAsync(new[] { command.Id }, cancellationToken);
            int attractionCount = counts.TryGetValue(command.Id, out int value) ? value : 0;

            return ApplicationResult<AttractionManufacturerResult>.Success(new AttractionManufacturerResult
            {
                Id = updated.Id,
                Name = updated.Name,
                Biography = updated.Biography,
                AttractionCount = attractionCount,
            });
        }
        catch
        {
            return ApplicationResult<AttractionManufacturerResult>.Failure(ApplicationError.Technical("attraction-manufacturer.update.failed", "Error while updating attraction manufacturer"));
        }
    }
}
