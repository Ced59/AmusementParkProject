namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IVisitRecapPublicParkReader
{
    Task<IReadOnlyDictionary<string, string>> GetVisibleNamesAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken);

    Task<string?> GetVisibleNameAsync(
        string parkId,
        CancellationToken cancellationToken);
}
