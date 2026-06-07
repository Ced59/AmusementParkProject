using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Ports;

public interface IParkGraphUpsertHistoryRepository
{
    Task SaveAsync(ParkGraphUpsertHistoryEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkGraphUpsertHistoryEntry>> ListRecentAsync(ParkGraphUpsertHistoryQuery query, CancellationToken cancellationToken);
}

public sealed class ParkGraphUpsertHistoryQuery
{
    public string? TargetParkId { get; init; }

    public int Limit { get; init; } = 20;
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
