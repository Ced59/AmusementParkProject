using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IAccountCommunityExportSource
{
    Task<AccountCommunityExportData> LoadAsync(
        string userId,
        CancellationToken cancellationToken);
}
