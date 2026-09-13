using System.ComponentModel.DataAnnotations;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class SubmitShareModerationReportRequestDto
{
    [Required]
    public string TargetType { get; init; } = string.Empty;

    [Required]
    public string ShareId { get; init; } = string.Empty;

    [Required]
    public string Reason { get; init; } = string.Empty;

    [MaxLength(ShareModerationReport.MaximumDetailsLength)]
    public string? Details { get; init; }
}
