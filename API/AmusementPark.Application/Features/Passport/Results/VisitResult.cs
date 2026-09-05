using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Results;

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
