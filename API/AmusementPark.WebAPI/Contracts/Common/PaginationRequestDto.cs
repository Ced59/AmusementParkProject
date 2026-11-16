using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.Common;

/// <summary>
/// Paramètres HTTP de pagination réutilisables.
/// </summary>
public sealed class PaginationRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; init; } = 1;

    public bool IncludeItems { get; init; } = true;

    [Range(1, 100, ErrorMessage = "Size must be between 1 and 100")]
    public int Size { get; init; } = 20;
}
