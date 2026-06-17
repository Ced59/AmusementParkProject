namespace AmusementPark.Application.Features.Contact.Contracts;

public sealed record ContactGrievanceSubmission(
    string? Message,
    string? Honeypot,
    string? LanguageCode,
    string? IpAddress,
    string? UserAgent);

public sealed record ContactGrievanceSubmissionResult(bool Accepted, DateTime? SubmittedAtUtc);

public sealed record ContactGrievanceSearchCriteria(
    string? Search,
    string? IpAddress,
    string? LanguageCode);
