namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportExportDownload(
    string FileName,
    string ContentType,
    byte[] Content,
    string ChecksumSha256);
