namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingEvidenceThresholdsDto
{
    public int Provisional { get; set; }

    public int Eligible { get; set; }

    public int Established { get; set; }

    public int Strong { get; set; }
}
