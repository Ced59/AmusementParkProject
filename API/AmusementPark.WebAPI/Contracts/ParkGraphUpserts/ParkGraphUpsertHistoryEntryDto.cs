using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

public sealed class ParkGraphUpsertHistoryEntryDto
{
    public string Id { get; set; } = string.Empty;

    public string OperationKind { get; set; } = "preview";

    public string? TargetParkId { get; set; }

    public string? TargetParkName { get; set; }

    public string? RequestedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string RawJson { get; set; } = string.Empty;

    public ParkGraphUpsertResultDto Result { get; set; } = new ParkGraphUpsertResultDto();
}
