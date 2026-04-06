namespace AmusementPark.WebAPI.Contracts.Common;

/// <summary>
/// Contrat HTTP de pagination aligné sur le legacy.
/// </summary>
public sealed class PaginationDto
{
    public int? TotalItems { get; set; }

    public int? TotalPages { get; set; }

    public int? CurrentPage { get; set; }

    public int? ItemsPerPage { get; set; }
}
