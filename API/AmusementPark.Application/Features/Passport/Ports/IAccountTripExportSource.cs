using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IAccountTripExportSource
{
    Task<IReadOnlyCollection<AccountTripExportData>> LoadAsync(
        string userId,
        CancellationToken cancellationToken);
}
