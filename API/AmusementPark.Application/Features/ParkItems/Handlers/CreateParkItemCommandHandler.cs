using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class CreateParkItemCommandHandler : ICommandHandler<CreateParkItemCommand, ApplicationResult<ParkItem>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ParkItemReferenceValidator parkItemReferenceValidator;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly ParkItemContentQualityService contentQualityService;
    private readonly IPublicSeoUpdateNotifier publicSeoUpdateNotifier;

    public CreateParkItemCommandHandler(
        IParkItemRepository parkItemRepository,
        ParkItemReferenceValidator parkItemReferenceValidator,
        ISearchProjectionWriter searchProjectionWriter,
        ParkItemContentQualityService contentQualityService,
        IPublicSeoUpdateNotifier publicSeoUpdateNotifier)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkItemReferenceValidator = parkItemReferenceValidator;
        this.searchProjectionWriter = searchProjectionWriter;
        this.contentQualityService = contentQualityService;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
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

        if (parkItem.IsVisible)
        {
            ParkItemContentQualityResult quality = this.contentQualityService.Evaluate(parkItem);
            if (!quality.IsPublishable)
            {
                return ApplicationResult<ParkItem>.Failure(ParkItemApplicationErrors.PublicationBlocked(quality.MissingRequirementKeys));
            }
        }

        try
        {
            ParkItem created = await this.parkItemRepository.CreateAsync(parkItem, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, created.Id, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, created.ParkId, cancellationToken);
            await this.publicSeoUpdateNotifier.NotifyAsync(
                new PublicSeoUpdate
                {
                    CurrentParkItems = PublicSeoParkItemSnapshot.FromParkItems(new[] { created }),
                },
                cancellationToken);
            return ApplicationResult<ParkItem>.Success(created);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<ParkItem>.Failure(ParkItemApplicationErrors.ErrorCreatingParkItem());
        }
    }
}
