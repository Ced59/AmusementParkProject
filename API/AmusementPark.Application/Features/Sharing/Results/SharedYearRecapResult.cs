namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record SharedYearRecapResult(
    DateTime PublishedAtUtc,
    YearRecapSharePreviewResult Content);
