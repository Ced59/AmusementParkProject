namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class VisitRecapShareDateDto
{
    public int Year { get; set; }

    public int? Month { get; set; }

    public int? Day { get; set; }

    public string Precision { get; set; } = string.Empty;

    public bool IsApproximate { get; set; }
}
