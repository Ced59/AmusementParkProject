using System;
using System.Collections.Generic;
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

    public string? ZoneId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ParkItemCategoryDto Category { get; set; }

    public ParkItemTypeDto Type { get; set; }

    public bool IsVisible { get; set; }

    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.Validated;

    public ParkItemContentQualityDto ContentQuality { get; set; } = new();

    public ParkItemAdminPublicationSignalsDto PublicationSignals { get; set; } = new();
}

public sealed class ParkItemContentQualityDto
{
    public bool StructureComplete { get; set; }

    public bool HasAnyDescription { get; set; }

    public bool HasFrenchDescription { get; set; }

    public bool HasEnglishDescription { get; set; }

    public bool HasZone { get; set; }

    public bool HasPreciseType { get; set; }

    public bool HasLocation { get; set; }

    public bool HasAccessConditions { get; set; }

    public bool IsPublishable { get; set; }

    public IReadOnlyCollection<string> AvailableLanguageCodes { get; set; } = Array.Empty<string>();

    public IReadOnlyCollection<string> MissingRequirementKeys { get; set; } = Array.Empty<string>();
}

public sealed class ParkItemAdminPublicationSignalsDto
{
    public bool IsVisible { get; set; }

    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.Validated;

    public DateTime? LastUpdatedAtUtc { get; set; }

    public IReadOnlyCollection<string> AvailableLanguageCodes { get; set; } = Array.Empty<string>();

    public bool IsPublishable { get; set; }
}
