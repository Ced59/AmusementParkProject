using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

public sealed class ParkItemsBulkCreateRequestDto
{
    public string ParkId { get; set; } = string.Empty;

    public IReadOnlyCollection<ParkItemBulkCreateDraftDto> Rows { get; set; } = new List<ParkItemBulkCreateDraftDto>();
}

public sealed class ParkItemBulkCreateDraftDto
{
    public int RowNumber { get; set; }

    public string? Name { get; set; }

    public ParkItemCategoryDto? Category { get; set; }

    public ParkItemTypeDto? Type { get; set; }

    public string? ZoneId { get; set; }

    public string? ZoneName { get; set; }

    public string? ManufacturerId { get; set; }

    public string? ManufacturerName { get; set; }

    public bool? IsVisible { get; set; }

    public AdminReviewStatusDto? AdminReviewStatus { get; set; }

    public string? DescriptionFr { get; set; }
}

public sealed class ParkItemsBulkCreatePreviewResultDto
{
    public IReadOnlyCollection<ParkItemBulkCreatePreviewRowDto> Rows { get; set; } = new List<ParkItemBulkCreatePreviewRowDto>();

    public int ReadyCount { get; set; }

    public int WarningCount { get; set; }

    public int ErrorCount { get; set; }
}

public sealed class ParkItemsBulkCreateApplyResultDto
{
    public IReadOnlyCollection<ParkItemBulkCreatePreviewRowDto> Rows { get; set; } = new List<ParkItemBulkCreatePreviewRowDto>();

    public IReadOnlyCollection<string> CreatedIds { get; set; } = new List<string>();

    public int RequestedCount { get; set; }

    public int CreatedCount { get; set; }

    public int IgnoredCount { get; set; }
}

public sealed class ParkItemBulkCreatePreviewRowDto
{
    public int RowNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public ParkItemCategoryDto Category { get; set; }

    public ParkItemTypeDto Type { get; set; }

    public string? ZoneId { get; set; }

    public string? ZoneName { get; set; }

    public string? ManufacturerId { get; set; }

    public string? ManufacturerName { get; set; }

    public bool IsVisible { get; set; }

    public AdminReviewStatusDto AdminReviewStatus { get; set; }

    public string? DescriptionFr { get; set; }

    public bool CanApply { get; set; }

    public IReadOnlyCollection<string> Errors { get; set; } = new List<string>();

    public IReadOnlyCollection<string> Warnings { get; set; } = new List<string>();
}
