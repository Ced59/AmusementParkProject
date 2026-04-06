using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Parks;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems;

/// <summary>
/// Valide les références externes utilisées par les park items.
/// </summary>
public sealed class ParkItemReferenceValidator
{
    private readonly IParkRepository parkRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IAttractionManufacturerRepository attractionManufacturerRepository;

    public ParkItemReferenceValidator(
        IParkRepository parkRepository,
        IParkZoneRepository parkZoneRepository,
        IAttractionManufacturerRepository attractionManufacturerRepository)
    {
        this.parkRepository = parkRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.attractionManufacturerRepository = attractionManufacturerRepository;
    }

    public async Task<ApplicationError?> EnsureParkExistsAsync(string parkId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parkId))
        {
            return ParkApplicationErrors.ParkNotExists();
        }

        Park? park = await this.parkRepository.GetByIdAsync(parkId.Trim(), true, cancellationToken);
        return park is null ? ParkApplicationErrors.ParkNotExists() : null;
    }

    public async Task<ApplicationError?> ValidateForWriteAsync(ParkItem parkItem, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkItem);

        ApplicationError? parkError = await this.EnsureParkExistsAsync(parkItem.ParkId, cancellationToken);
        if (parkError is not null)
        {
            return parkError;
        }

        if (!string.IsNullOrWhiteSpace(parkItem.ZoneId))
        {
            ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(parkItem.ZoneId.Trim(), cancellationToken);
            if (zone is null || !string.Equals(zone.ParkId, parkItem.ParkId, StringComparison.Ordinal))
            {
                return ParkZoneApplicationErrors.ParkZoneNotExists();
            }
        }

        string? manufacturerId = parkItem.AttractionDetails?.ManufacturerId;
        if (parkItem.Category == ParkItemCategory.Attraction && !string.IsNullOrWhiteSpace(manufacturerId))
        {
            AttractionManufacturer? manufacturer = await this.attractionManufacturerRepository.GetByIdAsync(
                manufacturerId.Trim(),
                cancellationToken);

            if (manufacturer is null)
            {
                return ParkItemApplicationErrors.AttractionManufacturerNotExists();
            }
        }

        return null;
    }
}
