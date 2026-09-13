namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ProfileComparisonYearDto
{
    public int Year { get; set; }

    public long CreatorVisitCount { get; set; }

    public long AcceptorVisitCount { get; set; }

    public long? CreatorRideCount { get; set; }

    public long? AcceptorRideCount { get; set; }
}
