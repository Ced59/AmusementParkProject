using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Commands;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de création d'un attraction manufacturer.
/// </summary>
public sealed class CreateAttractionManufacturerCommandHandler : ICommandHandler<CreateAttractionManufacturerCommand, ApplicationResult<AttractionManufacturerResult>>
{
    private readonly IAttractionManufacturerRepository repository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="CreateAttractionManufacturerCommandHandler"/>.
    /// </summary>
    public CreateAttractionManufacturerCommandHandler(IAttractionManufacturerRepository repository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<AttractionManufacturerResult>> HandleAsync(CreateAttractionManufacturerCommand command, CancellationToken cancellationToken = default)
    {
        if (command.AttractionManufacturer is null)
        {
            return ApplicationResult<AttractionManufacturerResult>.Failure(ApplicationErrors.Required(nameof(command.AttractionManufacturer)));
        }

        try
        {
            AttractionManufacturer created = await this.repository.CreateAsync(command.AttractionManufacturer, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, created.Id, cancellationToken);
            return ApplicationResult<AttractionManufacturerResult>.Success(new AttractionManufacturerResult
            {
                Id = created.Id,
                Name = created.Name,
                Biography = created.Biography,
                AdminReviewStatus = created.AdminReviewStatus,
                AttractionCount = 0,
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<AttractionManufacturerResult>.Failure(ApplicationError.Technical("attraction-manufacturer.create.failed", "Error while creating attraction manufacturer"));
        }
    }
}
