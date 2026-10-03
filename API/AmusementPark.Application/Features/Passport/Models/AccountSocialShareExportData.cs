namespace AmusementPark.Application.Features.Passport.Models;

public sealed record AccountSocialShareExportData(
    DateTime OccurredAtUtc,
    string TargetType,
    string? TargetTitle,
    string? LanguageCode,
    string Channel);
