using System.ComponentModel.DataAnnotations;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ReviewShareModerationReportRequestDto
{
    [Required]
    public string Decision { get; init; } = string.Empty;

    [MaxLength(ShareModerationReport.MaximumDecisionNoteLength)]
    public string? Note { get; init; }
}
