namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class BayesianRatingParametersDto
{
    public double PriorMean { get; set; }

    public int PriorWeight { get; set; }
}
