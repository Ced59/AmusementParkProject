using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IYearRecapShareSourceVersionProvider
{
    Task<ApplicationResult<YearRecapShareSourceRevision>> GetOwnedSourceVersionAsync(
        string ownerUserId,
        int year,
        CancellationToken cancellationToken);
}
