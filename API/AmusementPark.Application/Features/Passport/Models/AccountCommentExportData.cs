namespace AmusementPark.Application.Features.Passport.Models;

public sealed record AccountCommentExportData(
    string Reference,
    string TargetType,
    string? TargetName,
    string? ParkName,
    IReadOnlyCollection<AccountLocalizedTextExportData> Bodies,
    IReadOnlyCollection<string> ImageReferences,
    bool IsOfficial,
    string ModerationStatus,
    long Revision,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
