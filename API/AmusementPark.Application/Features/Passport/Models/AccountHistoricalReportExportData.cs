namespace AmusementPark.Application.Features.Passport.Models;

public sealed record AccountHistoricalReportExportData(
    string ParkName,
    int VisitYear,
    int? VisitMonth,
    int? VisitDay,
    string VisitDatePrecision,
    bool VisitDateIsApproximate,
    string ClaimedName,
    string? SourceUrl,
    string? SourceReference,
    string? Details,
    string Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? DecisionNote,
    long Revision);
