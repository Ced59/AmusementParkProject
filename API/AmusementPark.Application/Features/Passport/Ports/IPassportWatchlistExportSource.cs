using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IPassportWatchlistExportSource
{
    Task<PassportWatchlistExportData> LoadAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken);
}
