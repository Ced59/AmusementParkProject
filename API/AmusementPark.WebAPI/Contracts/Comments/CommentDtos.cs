using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Comments;

public sealed class CreateCommentRequestDto
{
    [Required]
    public string TargetType { get; set; } = string.Empty;

    [Required]
    public string TargetId { get; set; } = string.Empty;

    [Required]
    public List<LocalizedTextDto> Bodies { get; set; } = new List<LocalizedTextDto>();

    public bool IsOfficial { get; set; }
}

public sealed class UpdateCommentRequestDto
{
    [Required]
    public List<LocalizedTextDto> Bodies { get; set; } = new List<LocalizedTextDto>();

    public bool IsOfficial { get; set; }
}

public sealed class CommentDto
{
    public string Id { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string AuthorDisplayName { get; set; } = string.Empty;

    public string AuthorRole { get; set; } = string.Empty;

    public List<LocalizedTextDto> Bodies { get; set; } = new List<LocalizedTextDto>();

    public bool IsOfficial { get; set; }

    public bool CanUpdate { get; set; }

    public bool CanDelete { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class CommentSummaryDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public long CommentCount { get; set; }

    public CommentDto? OfficialComment { get; set; }
}

public sealed class CommentThreadDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public List<CommentDto> Comments { get; set; } = new List<CommentDto>();
}
