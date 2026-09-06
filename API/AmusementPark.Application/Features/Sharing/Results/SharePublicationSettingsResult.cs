namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record SharePublicationSettingsResult(
    bool IsPublic,
    string? ShareId,
    DateTime? PublishedAtUtc);
