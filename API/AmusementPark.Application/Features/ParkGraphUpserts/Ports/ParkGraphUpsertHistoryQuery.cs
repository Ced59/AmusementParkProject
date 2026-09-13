using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Ports;

public sealed class ParkGraphUpsertHistoryQuery
{
    public string? TargetParkId { get; init; }

    public int Limit { get; init; } = 20;
}
