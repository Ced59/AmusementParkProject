using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Contrat HTTP d'administration pour les listes paginées de park items.
/// </summary>
public sealed class ParkItemAdminListDto
{
    public string Id { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ParkItemCategoryDto Category { get; set; }

    public ParkItemTypeDto Type { get; set; }

    public bool IsVisible { get; set; }

    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.Ready;
}
