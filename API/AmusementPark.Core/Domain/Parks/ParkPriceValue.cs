namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkPriceValue
{
    public ParkPricingMode Mode { get; set; }

    public decimal? Amount { get; set; }

    public decimal? MinimumAmount { get; set; }

    public decimal? MaximumAmount { get; set; }
}
