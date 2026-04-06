namespace AmusementPark.Application.Common.Results;

/// <summary>
/// Alias sémantique de page de résultats pour les recherches applicatives.
/// </summary>
/// <typeparam name="TItem">Type d'élément retourné.</typeparam>
public sealed class SearchResultPage<TItem> : PagedResult<TItem>
{
    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="SearchResultPage{TItem}"/>.
    /// </summary>
    public SearchResultPage(IReadOnlyCollection<TItem> items, int page, int pageSize, long totalItems)
        : base(items, page, pageSize, totalItems)
    {
    }
}
