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
/// Handler de mise à jour de visibilité d'un parc.
/// </summary>
public sealed class UpdateParkVisibilityCommandHandler : ICommandHandler<UpdateParkVisibilityCommand, ApplicationResult<Park>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public UpdateParkVisibilityCommandHandler(IParkRepository parkRepository, IParkItemRepository parkItemRepository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<Park>> HandleAsync(UpdateParkVisibilityCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ParkId))
        {
            return ApplicationResult<Park>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        try
        {
            Park? updated = await this.parkRepository.UpdateVisibilityAsync(command.ParkId.Trim(), command.IsVisible, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<Park>.Failure(ParkApplicationErrors.ParkNotExists());
            }

            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, updated.Id, cancellationToken);
            IReadOnlyCollection<ParkItem> parkItems = await this.parkItemRepository.GetByParkIdAsync(updated.Id, true, cancellationToken);
            foreach (ParkItem parkItem in parkItems)
            {
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, parkItem.Id, cancellationToken);
            }

            return ApplicationResult<Park>.Success(updated);
        }
        catch
        {
            return ApplicationResult<Park>.Failure(ParkApplicationErrors.ErrorUpdatingPark());
        }
    }
}
