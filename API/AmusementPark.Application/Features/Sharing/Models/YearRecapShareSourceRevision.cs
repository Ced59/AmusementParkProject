namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record YearRecapShareSourceRevision(
    long Version,
    string SourceFingerprint);
