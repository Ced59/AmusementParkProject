using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Queries;

public sealed record ListParkGraphUpsertHistoryQuery(string? TargetParkId, int Limit) : IQuery<IReadOnlyCollection<ParkGraphUpsertHistoryEntry>>;
