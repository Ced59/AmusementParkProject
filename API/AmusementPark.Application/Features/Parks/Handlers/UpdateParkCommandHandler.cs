using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de mise à jour d'un parc.
/// </summary>
public sealed class UpdateParkCommandHandler : ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IPublicSeoUpdateNotifier publicSeoUpdateNotifier;

    public UpdateParkCommandHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IPublicSeoUpdateNotifier publicSeoUpdateNotifier)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
    }

    public async Task<ApplicationResult<Park>> HandleAsync(UpdateParkCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ParkId))
        {
            return ApplicationResult<Park>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        if (command.Park is null)
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.Required(nameof(command.Park)));
        }

        try
        {
            Park? existing = await this.parkRepository.GetByIdAsync(command.ParkId.Trim(), true, cancellationToken);
            if (existing is null)
            {
                return ApplicationResult<Park>.Failure(ParkApplicationErrors.ParkNotExists());
            }

            command.Park.Id = existing.Id;
            command.Park.CreatedAtUtc = existing.CreatedAtUtc;
            command.Park.CurrentLogoImageId = existing.CurrentLogoImageId;

            Park? updated = await this.parkRepository.UpdateAsync(command.ParkId.Trim(), command.Park, cancellationToken);
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

            await this.publicSeoUpdateNotifier.NotifyAsync(
                new PublicSeoUpdate
                {
                    PreviousParks = PublicSeoParkSnapshot.FromParks(new[] { existing }),
                    CurrentParks = PublicSeoParkSnapshot.FromParks(new[] { updated }),
                    IncludeDiscoveryPages = true,
                },
                cancellationToken);

            return ApplicationResult<Park>.Success(updated);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<Park>.Failure(ParkApplicationErrors.ErrorUpdatingPark());
        }
    }
}
