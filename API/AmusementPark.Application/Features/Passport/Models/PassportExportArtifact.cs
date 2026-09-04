namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportExportArtifact(
    string FileName,
    string ContentType,
    byte[] Content,
    int SchemaVersion,
    string ChecksumSha256);
