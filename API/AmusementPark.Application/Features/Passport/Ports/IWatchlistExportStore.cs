using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IWatchlistExportStore
{
    Task<PassportWatchlistStoredExportData> LoadAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken);
}
