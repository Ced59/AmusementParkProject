using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

public sealed class ParkGraphUpsertRequestDto
{
    public string? TargetParkId { get; set; }

    public bool CreateIfMissing { get; set; }

    public bool ReplaceCollections { get; set; }

    public JsonElement Document { get; set; }
}

public sealed class ParkGraphBulkExportRequestDto
{
    public string SelectionMode { get; set; } = "filtered";

    public List<string> ParkIds { get; set; } = new List<string>();

    public string? SearchTerm { get; set; }

    public bool? IsVisible { get; set; }

    public string? AdminReviewStatus { get; set; }

    public string? Type { get; set; }

    public string? AudienceClassification { get; set; }

    public string? CountryCode { get; set; }

    public bool? HasValidCoordinates { get; set; }

    public string? ClosedFilter { get; set; }

    public string? OpeningHoursStatus { get; set; }

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; }

    public List<string> Sections { get; set; } = new List<string>();
}

public sealed class ParkGraphBulkExportJobDto
{
    public string JobId { get; set; } = string.Empty;

    public string Status { get; set; } = "Queued";

    public int ProgressPercentage { get; set; }

    public string? Message { get; set; }

    public int? ExportedParkCount { get; set; }

    public int? ProcessedParkCount { get; set; }

    public string? FileName { get; set; }

    public long? ContentLength { get; set; }

    public string? DownloadUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public string? Error { get; set; }
}

public sealed class BulkParkGraphUpsertRequestDto
{
    public bool CreateIfMissing { get; set; }

    public bool ReplaceCollections { get; set; }

    public JsonElement Document { get; set; }
}

public sealed class ParkGraphUpsertHistoryEntryDto
{
    public string Id { get; set; } = string.Empty;

    public string OperationKind { get; set; } = "preview";

    public string? TargetParkId { get; set; }

    public string? TargetParkName { get; set; }

    public string? RequestedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string RawJson { get; set; } = string.Empty;

    public ParkGraphUpsertResultDto Result { get; set; } = new ParkGraphUpsertResultDto();
}

public sealed class ParkGraphUpsertResultDto
{
    public string OperationId { get; set; } = string.Empty;

    public string Mode { get; set; } = "merge";

    public bool IsApplied { get; set; }

    public bool CanApply { get; set; }

    public DateTime PreviewedAtUtc { get; set; }

    public DateTime? AppliedAtUtc { get; set; }

    public string? TargetParkId { get; set; }

    public string? TargetParkName { get; set; }

    public ParkGraphUpsertCountsDto Counts { get; set; } = new ParkGraphUpsertCountsDto();

    public List<ParkGraphUpsertChangeDto> Changes { get; set; } = new List<ParkGraphUpsertChangeDto>();

    public List<string> Warnings { get; set; } = new List<string>();

    public List<string> Errors { get; set; } = new List<string>();
}

public sealed class BulkParkGraphUpsertResultDto
{
    public string OperationId { get; set; } = string.Empty;

    public bool IsApplied { get; set; }

    public bool CanApply { get; set; }

    public DateTime PreviewedAtUtc { get; set; }

    public DateTime? AppliedAtUtc { get; set; }

    public ParkGraphUpsertCountsDto Counts { get; set; } = new ParkGraphUpsertCountsDto();

    public List<BulkParkGraphUpsertParkResultDto> Parks { get; set; } = new List<BulkParkGraphUpsertParkResultDto>();

    public List<string> Warnings { get; set; } = new List<string>();

    public List<string> Errors { get; set; } = new List<string>();
}

public sealed class BulkParkGraphUpsertParkResultDto
{
    public int Index { get; set; }

    public string? TargetParkId { get; set; }

    public string? TargetParkName { get; set; }

    public ParkGraphUpsertResultDto Result { get; set; } = new ParkGraphUpsertResultDto();
}

public sealed class ParkGraphUpsertCountsDto
{
    public int Created { get; set; }

    public int Updated { get; set; }

    public int Deleted { get; set; }

    public int Unchanged { get; set; }

    public int Warnings { get; set; }

    public int Errors { get; set; }
}

public sealed class ParkGraphUpsertChangeDto
{
    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? EntityKey { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string ChangeType { get; set; } = string.Empty;

    public string MatchedBy { get; set; } = string.Empty;

    public List<ParkGraphUpsertFieldChangeDto> Fields { get; set; } = new List<ParkGraphUpsertFieldChangeDto>();
}

public sealed class ParkGraphUpsertFieldChangeDto
{
    public string Field { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }
}
