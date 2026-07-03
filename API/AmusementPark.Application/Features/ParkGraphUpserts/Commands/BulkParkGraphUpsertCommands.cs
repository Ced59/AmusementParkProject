using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Commands;

public sealed record PreviewBulkParkGraphUpsertCommand(
    BulkParkGraphUpsertRequest Request,
    string? RequestedByUserId) : ICommand<ApplicationResult<BulkParkGraphUpsertResult>>;

public sealed record ApplyBulkParkGraphUpsertCommand(
    BulkParkGraphUpsertRequest Request,
    string? RequestedByUserId) : ICommand<ApplicationResult<BulkParkGraphUpsertResult>>;
