using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class CreateParkItemCommandHandler : ICommandHandler<CreateParkItemCommand, ApplicationResult<ParkItem>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ParkItemReferenceValidator parkItemReferenceValidator;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public CreateParkItemCommandHandler(
        IParkItemRepository parkItemRepository,
        ParkItemReferenceValidator parkItemReferenceValidator,
        ISearchProjectionWriter searchProjectionWriter)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkItemReferenceValidator = parkItemReferenceValidator;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<ParkItem>> HandleAsync(CreateParkItemCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ParkItem is null)
        {
            return ApplicationResult<ParkItem>.Failure(ApplicationErrors.Required(nameof(command.ParkItem)));
        }

        ParkItem parkItem = command.ParkItem;
        ParkItemNormalization.Normalize(parkItem);

        ApplicationError? validationError = await this.parkItemReferenceValidator.ValidateForWriteAsync(parkItem, cancellationToken);
        if (validationError is not null)
        {
            return ApplicationResult<ParkItem>.Failure(validationError);
        }

        try
        {
            ParkItem created = await this.parkItemRepository.CreateAsync(parkItem, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, created.Id, cancellationToken);
            return ApplicationResult<ParkItem>.Success(created);
        }
        catch
        {
            return ApplicationResult<ParkItem>.Failure(ParkItemApplicationErrors.ErrorCreatingParkItem());
        }
    }
}
