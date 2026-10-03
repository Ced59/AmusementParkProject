using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IFederatedAccountExportSource
{
    Task<FederatedAccountExportData> LoadAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken);
}
