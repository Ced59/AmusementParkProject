using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record UpdateVisitMetadataCommand(
    string UserId,
    string VisitId,
    int Year,
    int? Month,
    int? Day,
    VisitDatePrecision Precision,
    bool IsApproximate,
    string? TimeZoneId,
    LocalServiceDayConvention ServiceDayConvention,
    string? Title,
    string? PrivateNote,
    long ExpectedVersion) : ICommand<ApplicationResult<VisitResult>>;

public sealed record CompleteVisitCommand(
    string UserId,
    string VisitId,
    long ExpectedVersion) : ICommand<ApplicationResult<VisitResult>>;

public sealed record ReopenVisitCommand(
    string UserId,
    string VisitId,
    long ExpectedVersion) : ICommand<ApplicationResult<VisitResult>>;

public sealed record ArchiveVisitCommand(
    string UserId,
    string VisitId,
    long ExpectedVersion) : ICommand<ApplicationResult<VisitResult>>;
