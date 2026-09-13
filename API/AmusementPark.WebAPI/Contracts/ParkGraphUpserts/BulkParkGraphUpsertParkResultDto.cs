using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

public sealed class BulkParkGraphUpsertParkResultDto
{
    public int Index { get; set; }

    public string? TargetParkId { get; set; }

    public string? TargetParkName { get; set; }

    public ParkGraphUpsertResultDto Result { get; set; } = new ParkGraphUpsertResultDto();
}
