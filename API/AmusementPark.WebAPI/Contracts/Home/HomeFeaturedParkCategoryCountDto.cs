namespace AmusementPark.WebAPI.Contracts.Home;

/// <summary>
/// Compteur d'éléments visibles par catégorie pour une carte de parc mise en avant.
/// </summary>
public sealed class HomeFeaturedParkCategoryCountDto
{
    public string Category { get; set; } = string.Empty;

    public int Count { get; set; }
}
