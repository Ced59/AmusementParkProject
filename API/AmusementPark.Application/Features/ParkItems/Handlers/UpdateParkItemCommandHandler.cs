using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class UpdateParkItemCommandHandler : ICommandHandler<UpdateParkItemCommand, ApplicationResult<ParkItem>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ParkItemReferenceValidator parkItemReferenceValidator;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly ParkItemContentQualityService contentQualityService;

    public UpdateParkItemCommandHandler(
        IParkItemRepository parkItemRepository,
        ParkItemReferenceValidator parkItemReferenceValidator,
        ISearchProjectionWriter searchProjectionWriter,
        ParkItemContentQualityService contentQualityService)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkItemReferenceValidator = parkItemReferenceValidator;
        this.searchProjectionWriter = searchProjectionWriter;
        this.contentQualityService = contentQualityService;
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

        if (!existing.IsVisible && updatedState.IsVisible)
        {
            ParkItemContentQualityResult quality = this.contentQualityService.Evaluate(updatedState);
            if (!quality.IsPublishable)
            {
                return ApplicationResult<ParkItem>.Failure(ParkItemApplicationErrors.PublicationBlocked(quality.MissingRequirementKeys));
            }
        }

        try
        {
            ParkItem? updated = await this.parkItemRepository.UpdateAsync(command.ParkItemId, updatedState, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<ParkItem>.Failure(ParkItemApplicationErrors.ParkItemNotExists());
            }

            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, updated.Id, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, updated.ParkId, cancellationToken);
            if (!string.Equals(existing.ParkId, updated.ParkId, StringComparison.Ordinal))
            {
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, existing.ParkId, cancellationToken);
            }

            return ApplicationResult<ParkItem>.Success(updated);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<ParkItem>.Failure(ParkItemApplicationErrors.ErrorUpdatingParkItem());
        }
    }
}
