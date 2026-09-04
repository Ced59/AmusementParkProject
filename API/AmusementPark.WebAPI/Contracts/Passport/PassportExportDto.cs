namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed record PassportExportDto(
    string Id,
    string Format,
    string Status,
    int SchemaVersion,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? CompletedAtUtc,
    string? FileName,
    long? SizeBytes,
    string? ErrorCode,
    string? DownloadUrl);
