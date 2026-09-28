namespace AmusementPark.Application.Features.Seo.Models;

public sealed record HistoricalKeyYearSitemapCandidate(
    string ParkId,
    string ParkName,
    int Year,
    DateTime LastModifiedAtUtc);
