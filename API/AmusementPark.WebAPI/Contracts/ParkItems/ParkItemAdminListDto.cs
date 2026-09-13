using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DataCompletenessScoreDto? DataCompleteness { get; set; }
}
