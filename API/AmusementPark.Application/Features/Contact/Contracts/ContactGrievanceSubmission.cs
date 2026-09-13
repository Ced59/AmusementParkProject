namespace AmusementPark.Application.Features.Contact.Contracts;

public sealed record ContactGrievanceSubmission(
    string? Message,
    string? Honeypot,
    string? LanguageCode,
    string? IpAddress,
    string? UserAgent);
