using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record RideOccurrenceCreationItem(
    string ParkItemId,
    TimeOnly? LocalTime,
    bool IsApproximate,
    RideOccurrenceStatus Status,
    string? PrivateNote,
    bool ConfirmHistoricalConflict,
    int Count = 1);

public sealed record AddRideOccurrencesBatchCommand(
    string UserId,
    string VisitId,
    string ClientOperationId,
    IReadOnlyCollection<RideOccurrenceCreationItem?> Items)
    : ICommand<ApplicationResult<CreateRideOccurrencesResult>>;

public sealed record UpdateRideOccurrenceCommand(
    string UserId,
    string VisitId,
    string OccurrenceId,
    long ExpectedVersion,
    TimeOnly? LocalTime,
    bool IsApproximate,
    RideOccurrenceStatus Status,
    string? PrivateNote,
    bool ConfirmHistoricalConflict)
    : ICommand<ApplicationResult<RideOccurrenceResult>>;

public sealed record DeleteRideOccurrenceCommand(
    string UserId,
    string VisitId,
    string OccurrenceId,
    long ExpectedVersion)
    : ICommand<ApplicationResult<RideOccurrenceResult>>;

public sealed record ReorderRideOccurrenceCommand(
    string UserId,
    string VisitId,
    string ClientOperationId,
    string OccurrenceId,
    long ExpectedVersion,
    string? AnchorOccurrenceId,
    RideOccurrencePlacement Placement)
    : ICommand<ApplicationResult<ReorderRideOccurrenceResult>>;
