namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IVisitRecapPublicParkReader
{
    Task<string?> GetVisibleNameAsync(
        string parkId,
        CancellationToken cancellationToken);
}
