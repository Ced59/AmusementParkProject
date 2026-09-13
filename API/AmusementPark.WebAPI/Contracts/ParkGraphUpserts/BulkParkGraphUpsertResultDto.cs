using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

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
