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
