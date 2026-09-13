namespace AmusementPark.Application.Features.ParkGraphUpserts.Results;

public sealed class BulkParkGraphUpsertParkResult
{
    public int Index { get; set; }

    public string? TargetParkId { get; set; }

    public string? TargetParkName { get; set; }

    public ParkGraphUpsertResult Result { get; set; } = new ParkGraphUpsertResult();
}
