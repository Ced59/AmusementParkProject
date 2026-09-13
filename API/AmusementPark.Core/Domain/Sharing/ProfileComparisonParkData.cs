namespace AmusementPark.Core.Domain.Sharing;

public sealed record ProfileComparisonParkData(
    string Name,
    string? CountryCode,
    long VisitCount);
