using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class UpdateParkItemCommandHandler : ICommandHandler<UpdateParkItemCommand, ApplicationResult<ParkItem>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ParkItemReferenceValidator parkItemReferenceValidator;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public UpdateParkItemCommandHandler(
        IParkItemRepository parkItemRepository,
        ParkItemReferenceValidator parkItemReferenceValidator,
        ISearchProjectionWriter searchProjectionWriter)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkItemReferenceValidator = parkItemReferenceValidator;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<ParkItem>> HandleAsync(UpdateParkItemCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ParkItemId))
        {
            return ApplicationResult<ParkItem>.Failure(ApplicationErrors.Required(nameof(command.ParkItemId)));
        }

        if (command.ParkItem is null)
        {
            return ApplicationResult<ParkItem>.Failure(ApplicationErrors.Required(nameof(command.ParkItem)));
        }

        ParkItem? existing = await this.parkItemRepository.GetByIdAsync(command.ParkItemId, cancellationToken);
        if (existing is null)
        {
            return ApplicationResult<ParkItem>.Failure(ParkItemApplicationErrors.ParkItemNotExists());
        }

        ParkItem updatedState = command.ParkItem;
        updatedState.Id = existing.Id;
        updatedState.CreatedAtUtc = existing.CreatedAtUtc;
        updatedState.UpdatedAtUtc = existing.UpdatedAtUtc;

        ParkItemNormalization.Normalize(updatedState);

        ApplicationError? validationError = await this.parkItemReferenceValidator.ValidateForWriteAsync(updatedState, cancellationToken);
        if (validationError is not null)
        {
            return ApplicationResult<ParkItem>.Failure(validationError);
        }

        try
        {
            ParkItem? updated = await this.parkItemRepository.UpdateAsync(command.ParkItemId, updatedState, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<ParkItem>.Failure(ParkItemApplicationErrors.ParkItemNotExists());
            }

            await this.searchProjectionWriter.UpsertAsync("parkItems", updated.Id, cancellationToken);
            return ApplicationResult<ParkItem>.Success(updated);
        }
        catch
        {
            return ApplicationResult<ParkItem>.Failure(ParkItemApplicationErrors.ErrorUpdatingParkItem());
        }
    }
}
