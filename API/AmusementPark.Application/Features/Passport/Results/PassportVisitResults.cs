using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record VisitDateResult(
    int Year,
    int? Month,
    int? Day,
    VisitDatePrecision Precision,
    bool IsApproximate);

public sealed record VisitParkAssessmentResult(
    double Value,
    string? PrivateComment,
    int Revision,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record VisitResult(
    string Id,
    string ParkId,
    VisitDateResult Date,
    string? TimeZoneId,
    LocalServiceDayConvention ServiceDayConvention,
    VisitStatus Status,
    VisitPrivacy Privacy,
    string? Title,
    string? PrivateNote,
    long Version,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    VisitParkAssessmentResult? ParkAssessment = null,
    string? ParkName = null);

public sealed record CreateVisitResult(
    VisitResult Visit,
    bool WasReplayed);

public sealed record VisitPageResult(
    IReadOnlyCollection<VisitResult> Items,
    UserVisitListCursor? NextCursor);
