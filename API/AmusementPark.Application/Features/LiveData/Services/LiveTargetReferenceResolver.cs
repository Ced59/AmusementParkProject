using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveTargetReferenceResolver
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public LiveTargetReferenceResolver(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<LiveTargetReference?> ResolveAsync(
        LiveTargetType type,
        string targetId,
        string parkId,
        CancellationToken cancellationToken)
    {
        string normalizedTargetId = targetId?.Trim() ?? string.Empty;
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        if (normalizedTargetId.Length == 0
            || normalizedParkId.Length == 0
            || !Enum.IsDefined(type))
        {
            return null;
        }

        Park? park = await this.parkRepository.GetByIdAsync(
            normalizedParkId,
            true,
            cancellationToken);
        if (park is null
            || string.IsNullOrWhiteSpace(park.Name)
            || string.IsNullOrWhiteSpace(park.CountryCode))
        {
            return null;
        }

        try
        {
            if (type == LiveTargetType.Park)
            {
                return string.Equals(normalizedTargetId, normalizedParkId, StringComparison.Ordinal)
                    ? new LiveTargetReference(
                        type,
                        normalizedTargetId,
                        normalizedParkId,
                        park.Name,
                        park.Name,
                        park.CountryCode)
                    : null;
            }

            ParkItem? item = await this.parkItemRepository.GetByIdAsync(
                normalizedTargetId,
                true,
                cancellationToken);
            if (item is null
                || string.IsNullOrWhiteSpace(item.Name)
                || !string.Equals(item.ParkId, normalizedParkId, StringComparison.Ordinal))
            {
                return null;
            }

            return new LiveTargetReference(
                type,
                normalizedTargetId,
                normalizedParkId,
                item.Name,
                park.Name,
                park.CountryCode);
        }
        catch (LiveDataValidationException)
        {
            return null;
        }
    }
}
