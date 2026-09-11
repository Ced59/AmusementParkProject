using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IYearRecapSourceReader
{
    Task<YearRecapSourceData> ReadOwnedCompletedYearAsync(
        string ownerUserId,
        int year,
        CancellationToken cancellationToken);
}
