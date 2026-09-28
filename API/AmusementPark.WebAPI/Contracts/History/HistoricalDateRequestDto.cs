namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoricalDateRequestDto
{
    public int Year { get; init; }

    public int? Month { get; init; }

    public int? Day { get; init; }

    public string Precision { get; init; } = string.Empty;

    public bool IsApproximate { get; init; }

    public string? Qualifier { get; init; }
}
