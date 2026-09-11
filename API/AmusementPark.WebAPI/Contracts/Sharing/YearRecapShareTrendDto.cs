namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class YearRecapShareTrendDto
{
    public string Name { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public long FirstWindowRatingCount { get; set; }

    public long LastWindowRatingCount { get; set; }

    public double FirstWindowAverage { get; set; }

    public double LastWindowAverage { get; set; }

    public double Delta { get; set; }
}
