namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileShareRatingSummaryDto
{
    public long RatedCount { get; set; }

    public long EligibleCount { get; set; }

    public double? Average { get; set; }
}
