namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class ParkRatingCompositionDto
{
    public double DirectRatingWeight { get; set; }

    public double ItemRatingWeight { get; set; }

    public bool BalancesItemCategoriesEqually { get; set; }

    public int MinimumEligibleItems { get; set; }

    public int MinimumItemsPerCategory { get; set; }

    public int MinimumCategories { get; set; }
}
