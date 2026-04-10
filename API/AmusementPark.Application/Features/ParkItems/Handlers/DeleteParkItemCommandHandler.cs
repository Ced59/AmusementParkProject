using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class DeleteParkItemCommandHandler : ICommandHandler<DeleteParkItemCommand, ApplicationResult>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public DeleteParkItemCommandHandler(IParkItemRepository parkItemRepository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult> HandleAsync(DeleteParkItemCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ParkItemId))
        {
            return ApplicationResult.Failure(ApplicationErrors.Required(nameof(command.ParkItemId)));
        }

        try
        {
            bool deleted = await this.parkItemRepository.DeleteAsync(command.ParkItemId, cancellationToken);
            if (!deleted)
            {
                return ApplicationResult.Failure(ParkItemApplicationErrors.ParkItemNotExists());
            }

            await this.searchProjectionWriter.DeleteAsync(SearchProjectionResourceTypes.ParkItems, command.ParkItemId, cancellationToken);
            return ApplicationResult.Success();
        }
        catch
        {
            return ApplicationResult.Failure(ParkItemApplicationErrors.ErrorDeletingParkItem());
        }
    }
}
