using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IWatchlistExportStore
{
    Task<PassportWatchlistStoredExportData> LoadAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FactualChangeEvent>> LoadFactualEventsAsync(
        IReadOnlyCollection<FactualChangeEventId> eventIds,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken);
}
