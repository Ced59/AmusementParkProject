namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportExport(
    string Id,
    string UserId,
    PassportExportFormat Format,
    PassportExportStatus Status,
    int SchemaVersion,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? CompletedAtUtc = null,
    string? FileName = null,
    string? ContentType = null,
    long? SizeBytes = null,
    string? ChecksumSha256 = null,
    string? ErrorCode = null);
