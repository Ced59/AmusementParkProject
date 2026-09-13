namespace AmusementPark.Application.Features.Contact.Contracts;

public sealed record ContactGrievanceSubmissionResult(bool Accepted, DateTime? SubmittedAtUtc);
