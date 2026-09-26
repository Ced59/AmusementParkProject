namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalDateDto
{
    public int Year { get; set; }

    public int? Month { get; set; }

    public int? Day { get; set; }

    public string Precision { get; set; } = string.Empty;

    public bool IsApproximate { get; set; }

    public string? Qualifier { get; set; }
}
