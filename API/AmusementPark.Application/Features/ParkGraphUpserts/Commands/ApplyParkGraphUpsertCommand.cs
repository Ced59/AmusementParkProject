using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Commands;

public sealed record ApplyParkGraphUpsertCommand(ParkGraphUpsertRequest Request, string? RequestedByUserId) : ICommand<ApplicationResult<ParkGraphUpsertResult>>;
