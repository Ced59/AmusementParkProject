namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPassportProfileShareScopeRegistry
{
    Task RegisterAsync(
        string ownerUserId,
        string scopeKey,
        IReadOnlyCollection<int> selectedYears,
        IReadOnlyCollection<string> selectedParkIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> ResolveScopeKeysAsync(
        string ownerUserId,
        IReadOnlyCollection<(string ParkId, int Year)> segments,
        CancellationToken cancellationToken);
}
