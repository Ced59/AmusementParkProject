namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record SharedVisitRecapResult(
    DateTime PublishedAtUtc,
    VisitRecapSharePreviewResult Content);
