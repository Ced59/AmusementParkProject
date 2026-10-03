namespace AmusementPark.Application.Features.Passport.Models;

public sealed record AccountIdentityExportData(
    string? FirstName,
    string? LastName,
    string? PublicDisplayName,
    string? Email,
    bool IsActivated,
    bool IsBlocked,
    string? PreferredLanguage,
    string? PreferredMeasurementSystem,
    bool HasAvatar,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<AccountLinkedLoginExportData> LinkedLogins,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime LastLoginUtc,
    DateTime LastActivityUtc);
