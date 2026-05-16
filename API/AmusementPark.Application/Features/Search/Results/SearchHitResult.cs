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
    public string? City { get; init; }
    public string? CountryCode { get; init; }
    public string? LogoImageId { get; init; }
    public int? AttractionCount { get; init; }
    public string? ParentParkId { get; init; }
    public string? ParentParkName { get; init; }
    public double? Score { get; init; }
}
