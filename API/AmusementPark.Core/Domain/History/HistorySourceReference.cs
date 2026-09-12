namespace AmusementPark.Core.Domain.History;

public sealed class HistorySourceReference
{
    public string? Label { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? AccessedAt { get; set; }
}
