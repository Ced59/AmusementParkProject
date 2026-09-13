using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ShareModerationReportSearchRequestDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int Size { get; init; } = 20;

    public string? Status { get; init; }

    public string? TargetType { get; init; }

    public string? Reason { get; init; }
}
