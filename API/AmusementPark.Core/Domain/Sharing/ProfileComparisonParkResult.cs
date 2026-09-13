namespace AmusementPark.Core.Domain.Sharing;

public sealed record ProfileComparisonParkResult(
    string Name,
    string? CountryCode,
    long? CreatorVisitCount,
    long? AcceptorVisitCount);
