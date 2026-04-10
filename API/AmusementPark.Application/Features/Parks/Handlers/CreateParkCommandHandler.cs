using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de création d'un parc.
/// </summary>
public sealed class CreateParkCommandHandler : ICommandHandler<CreateParkCommand, ApplicationResult<Park>>
{
    private readonly IParkRepository parkRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public CreateParkCommandHandler(IParkRepository parkRepository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.parkRepository = parkRepository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<Park>> HandleAsync(CreateParkCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Park is null)
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.Required(nameof(command.Park)));
        }

        try
        {
            Park created = await this.parkRepository.CreateAsync(command.Park, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, created.Id, cancellationToken);
            return ApplicationResult<Park>.Success(created);
        }
        catch
        {
            return ApplicationResult<Park>.Failure(ParkApplicationErrors.ErrorCreatingPark());
        }
    }
}
