namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingScaleDto
{
    public decimal Minimum { get; set; }

    public decimal Maximum { get; set; }

    public decimal Step { get; set; }
}
