namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record PassportProfileShareSourceRevision(
    long Version,
    string SourceFingerprint);
