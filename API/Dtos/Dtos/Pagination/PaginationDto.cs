namespace Dtos.Pagination;

public class PaginationDto
{
    public int? TotalItems { get; set; }
    public int? TotalPages { get; set; }
    public int? CurrentPage { get; set; }
    public int? ItemsPerPage { get; set; }


    public static PaginationDto Create(int? totalItems, int? currentPage, int? itemsPerPage)
    {
        if (totalItems.HasValue && currentPage.HasValue && itemsPerPage.HasValue && totalItems.Value > 0)
        {
            var totalPages = (totalItems.Value + itemsPerPage.Value - 1) / itemsPerPage.Value;
            return new PaginationDto
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                ItemsPerPage = itemsPerPage
            };
        }

        return null;
    }
}