namespace AmusementPark.Application.Features.Passport.Models;

public sealed record AccountImageExportData(
    string Reference,
    string? OriginalFileName,
    string? ContentType,
    string? Description,
    IReadOnlyCollection<AccountLocalizedTextExportData> AlternativeTexts,
    IReadOnlyCollection<AccountLocalizedTextExportData> Captions,
    IReadOnlyCollection<AccountLocalizedTextExportData> Credits,
    int Width,
    int Height,
    long SizeInBytes,
    bool IsPublished,
    string? SourceUrl,
    double? Latitude,
    double? Longitude,
    string? CameraMaker,
    string? CameraModel,
    DateTime? TakenOnUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
