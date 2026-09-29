using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Ports;

public interface IStandaloneAttractionOpeningHoursRepository
{
    Task<ParkOpeningHoursSchedule?> GetByStandaloneAttractionIdAsync(
        string standaloneAttractionId,
        CancellationToken cancellationToken);

    Task<ParkOpeningHoursSchedule> UpsertAsync(
        ParkOpeningHoursSchedule schedule,
        CancellationToken cancellationToken);
}
