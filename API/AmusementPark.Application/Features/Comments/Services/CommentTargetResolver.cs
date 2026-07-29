using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Comments.Services;

public sealed class CommentTargetResolver
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public CommentTargetResolver(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<CommentTargetMetadataResult?> ResolveAsync(
        CommentTargetType targetType,
        string targetId,
        bool includeHidden,
        CancellationToken cancellationToken)
    {
        if (targetType == CommentTargetType.Park)
        {
            Park? park = await this.parkRepository.GetByIdAsync(targetId, includeHidden, cancellationToken);
            if (park is null || string.IsNullOrWhiteSpace(park.Id))
            {
                return null;
            }

            string parkId = park.Id.Trim();
            return new CommentTargetMetadataResult(
                CommentTargetType.Park,
                parkId,
                park.Name?.Trim() ?? parkId,
                parkId,
                park.Name?.Trim());
        }

        if (targetType == CommentTargetType.ParkItem)
        {
            ParkItem? parkItem = await this.parkItemRepository.GetByIdAsync(targetId, includeHidden, cancellationToken);
            if (parkItem is null || string.IsNullOrWhiteSpace(parkItem.Id) || string.IsNullOrWhiteSpace(parkItem.ParkId))
            {
                return null;
            }

            Park? park = await this.parkRepository.GetByIdAsync(parkItem.ParkId, includeHidden, cancellationToken);
            if (park is null || string.IsNullOrWhiteSpace(park.Id))
            {
                return null;
            }

            return new CommentTargetMetadataResult(
                CommentTargetType.ParkItem,
                parkItem.Id.Trim(),
                parkItem.Name.Trim(),
                park.Id.Trim(),
                park.Name?.Trim());
        }

        return null;
    }
}
