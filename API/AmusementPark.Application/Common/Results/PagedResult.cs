namespace AmusementPark.Application.Common.Results;

/// <summary>
/// Représente une page de résultats applicatifs.
/// </summary>
/// <typeparam name="TItem">Type d'élément paginé.</typeparam>
public class PagedResult<TItem>
{
    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="PagedResult{TItem}"/>.
    /// </summary>
    /// <param name="items">Éléments de la page courante.</param>
    /// <param name="page">Numéro de page courant.</param>
    /// <param name="pageSize">Taille de page.</param>
    /// <param name="totalItems">Nombre total d'éléments.</param>
    public PagedResult(IReadOnlyCollection<TItem> items, int page, int pageSize, long totalItems)
    {
        this.Items = items;
        this.Page = page;
        this.PageSize = pageSize;
        this.TotalItems = totalItems;
    }

    /// <summary>
    /// Obtient les éléments de la page courante.
    /// </summary>
    public IReadOnlyCollection<TItem> Items { get; }

    /// <summary>
    /// Obtient le numéro de page courant.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Obtient la taille de page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Obtient le nombre total d'éléments.
    /// </summary>
    public long TotalItems { get; }

    /// <summary>
    /// Obtient le nombre total de pages.
    /// </summary>
    public int TotalPages
    {
        get
        {
            if (this.PageSize <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(this.TotalItems / (double)this.PageSize);
        }
    }
}
