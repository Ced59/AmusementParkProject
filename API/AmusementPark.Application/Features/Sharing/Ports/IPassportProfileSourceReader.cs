using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPassportProfileSourceReader
{
    Task<IReadOnlyCollection<PassportVisitStatisticsObservation>> ReadOwnedCompletedVisitsAsync(
        string ownerUserId,
        CancellationToken cancellationToken);

    Task<PassportProfileSourceData> ReadOwnedCompletedPassportAsync(
        string ownerUserId,
        CancellationToken cancellationToken);
}
