using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class DeleteParkItemCommandHandler : ICommandHandler<DeleteParkItemCommand, ApplicationResult>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IPublicSeoUpdateNotifier publicSeoUpdateNotifier;

    public DeleteParkItemCommandHandler(
        IParkItemRepository parkItemRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IPublicSeoUpdateNotifier publicSeoUpdateNotifier)
    {
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
    }

    public async Task<ApplicationResult> HandleAsync(DeleteParkItemCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ParkItemId))
        {
            return ApplicationResult.Failure(ApplicationErrors.Required(nameof(command.ParkItemId)));
        }

        try
        {
            AmusementPark.Core.Domain.Parks.ParkItem? existing = await this.parkItemRepository.GetByIdAsync(command.ParkItemId, cancellationToken);
            bool deleted = await this.parkItemRepository.DeleteAsync(command.ParkItemId, cancellationToken);
            if (!deleted)
            {
                return ApplicationResult.Failure(ParkItemApplicationErrors.ParkItemNotExists());
            }

            await this.searchProjectionWriter.DeleteAsync(SearchProjectionResourceTypes.ParkItems, command.ParkItemId, cancellationToken);
            if (existing is not null && !string.IsNullOrWhiteSpace(existing.ParkId))
            {
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, existing.ParkId, cancellationToken);
            }

            await this.publicSeoUpdateNotifier.NotifyAsync(
                new PublicSeoUpdate
                {
                    PreviousParkItems = PublicSeoParkItemSnapshot.FromParkItems(new[] { existing }),
                },
                cancellationToken);

            return ApplicationResult.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult.Failure(ParkItemApplicationErrors.ErrorDeletingParkItem());
        }
    }
}
