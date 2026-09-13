namespace AmusementPark.Core.Domain.Sharing;

public sealed record ProfileComparisonYearResult(
    int Year,
    long CreatorVisitCount,
    long AcceptorVisitCount,
    long? CreatorRideCount,
    long? AcceptorRideCount);
