namespace AmusementPark.Application.Features.Passport.Models;

public sealed record AccountLinkedLoginExportData(
    string Provider,
    string? Email,
    bool IsEmailVerified,
    string? DisplayName,
    DateTime LinkedAtUtc,
    DateTime LastLoginAtUtc);
