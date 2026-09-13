namespace AmusementPark.Application.Features.Contact.Contracts;

public sealed record ContactGrievanceSearchCriteria(
    string? Search,
    string? IpAddress,
    string? LanguageCode);
