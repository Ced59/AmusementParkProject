using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ContextualBlocks.Commands;
using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ContextualBlocks.Handlers;

public sealed class PreviewContextualBlockJsonCommandHandler
    : ICommandHandler<PreviewContextualBlockJsonCommand, ApplicationResult<ContextualBlockPreviewResult>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public PreviewContextualBlockJsonCommandHandler(IParkRepository parkRepository, IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<ContextualBlockPreviewResult>> HandleAsync(PreviewContextualBlockJsonCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.BlockType))
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(ApplicationErrors.Required("blockType"));
        }

        if (string.IsNullOrWhiteSpace(command.EntityId))
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(ApplicationErrors.Required("entityId"));
        }

        string blockType = command.BlockType.Trim();
        if (!ContextualBlockContracts.IsSupportedBlockType(blockType))
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(ContextualBlockApplicationErrors.UnsupportedBlockType(blockType));
        }

        string entityId = command.EntityId.Trim();
        if (IsParkItemBlockType(blockType))
        {
            ParkItem? item = await this.parkItemRepository.GetByIdAsync(entityId, true, cancellationToken);
            if (item is null)
            {
                return ApplicationResult<ContextualBlockPreviewResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkItem), entityId));
            }

            ContextualBlockPreviewResult itemResult = string.Equals(blockType, ContextualBlockContracts.ParkItemDescriptionBlockType, StringComparison.Ordinal)
                ? ContextualBlockJsonPreviewBuilder.PreviewParkItemDescription(blockType, item, command.Document)
                : ContextualBlockJsonPreviewBuilder.PreviewParkItemLocation(blockType, item, command.Document);
            return ApplicationResult<ContextualBlockPreviewResult>.Success(itemResult);
        }

        Park? park = await this.parkRepository.GetByIdAsync(entityId, true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), entityId));
        }

        ContextualBlockPreviewResult result;
        if (string.Equals(blockType, ContextualBlockContracts.ParkDescriptionBlockType, StringComparison.Ordinal))
        {
            result = ContextualBlockJsonPreviewBuilder.PreviewParkDescription(blockType, park, command.Document);
        }
        else if (string.Equals(blockType, ContextualBlockContracts.ParkLocationBlockType, StringComparison.Ordinal))
        {
            result = ContextualBlockJsonPreviewBuilder.PreviewParkLocation(blockType, park, command.Document);
        }
        else
        {
            result = ContextualBlockJsonPreviewBuilder.PreviewParkPractical(blockType, park, command.Document);
        }

        return ApplicationResult<ContextualBlockPreviewResult>.Success(result);
    }

    private static bool IsParkItemBlockType(string blockType)
    {
        return string.Equals(blockType, ContextualBlockContracts.ParkItemDescriptionBlockType, StringComparison.Ordinal)
            || string.Equals(blockType, ContextualBlockContracts.ParkItemLocationBlockType, StringComparison.Ordinal);
    }
}
