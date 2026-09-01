using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

internal static class RatingTargetMetadataResolver
{
    public static async Task<RatingTargetMetadataResult?> ResolveAsync(
        RatingTargetType targetType,
        string targetId,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        CancellationToken cancellationToken)
    {
        if (targetType == RatingTargetType.Park)
        {
            Park? park = await parkRepository.GetByIdAsync(targetId, false, cancellationToken);
            if (park is null || string.IsNullOrWhiteSpace(park.Id))
            {
                return null;
            }

            return new RatingTargetMetadataResult(
                RatingTargetType.Park,
                park.Id.Trim(),
                park.Name?.Trim() ?? park.Id.Trim(),
                park.Id.Trim(),
                park.Name?.Trim(),
                null,
                null,
                park.Status.CanReceiveVisitorRatings());
        }

        if (targetType == RatingTargetType.ParkItem)
        {
            ParkItem? item = await parkItemRepository.GetByIdAsync(
                targetId,
                false,
                cancellationToken);
            if (item is null
                || string.IsNullOrWhiteSpace(item.Id)
                || string.IsNullOrWhiteSpace(item.ParkId))
            {
                return null;
            }

            Park? park = await parkRepository.GetByIdAsync(item.ParkId, false, cancellationToken);
            if (park is null || string.IsNullOrWhiteSpace(park.Id))
            {
                return null;
            }

            bool canReceiveVisitorRatings = park.Status.CanReceiveVisitorRatings()
                && ParkItemStatusNormalizer.CanReceiveVisitorRatings(
                    item.Category,
                    item.AttractionDetails?.Status);

            return new RatingTargetMetadataResult(
                RatingTargetType.ParkItem,
                item.Id.Trim(),
                item.Name.Trim(),
                park.Id.Trim(),
                park.Name?.Trim(),
                item.Category,
                item.Type,
                canReceiveVisitorRatings);
        }

        return null;
    }
}
