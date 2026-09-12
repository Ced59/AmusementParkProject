namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record SharedPassportProfileResult(
    DateTime PublishedAtUtc,
    PassportProfileSharePreviewResult Content);
