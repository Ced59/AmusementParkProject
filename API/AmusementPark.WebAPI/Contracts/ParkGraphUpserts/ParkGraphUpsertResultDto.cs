using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

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

    public string? TargetStandaloneAttractionId { get; set; }

    public string? TargetStandaloneAttractionName { get; set; }

    public ParkGraphUpsertCountsDto Counts { get; set; } = new ParkGraphUpsertCountsDto();

    public List<ParkGraphUpsertChangeDto> Changes { get; set; } = new List<ParkGraphUpsertChangeDto>();

    public List<string> Warnings { get; set; } = new List<string>();

    public List<string> Errors { get; set; } = new List<string>();
}
