using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Ports;

public interface IParkGraphUpsertHistoryRepository
{
    Task SaveAsync(ParkGraphUpsertHistoryEntry entry, CancellationToken cancellationToken);
}

public sealed class ParkGraphUpsertHistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string OperationKind { get; set; } = "preview";

    public string? TargetParkId { get; set; }

    public string? TargetParkName { get; set; }

    public string? RequestedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string RawJson { get; set; } = string.Empty;

    public ParkGraphUpsertResult Result { get; set; } = new ParkGraphUpsertResult();
}
