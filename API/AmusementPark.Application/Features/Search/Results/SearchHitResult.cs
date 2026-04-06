namespace AmusementPark.Application.Features.Search.Results;

/// <summary>
/// Représente un hit de recherche applicatif.
/// </summary>
public sealed class SearchHitResult
{
    public string Id { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    public double? Score { get; init; }
}
