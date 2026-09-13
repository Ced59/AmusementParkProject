using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Contracts.Comments;

public sealed class CommentDto
{
    public string Id { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string AuthorDisplayName { get; set; } = string.Empty;

    public string? AuthorAvatarUrl { get; set; }

    public string AuthorRole { get; set; } = string.Empty;

    public List<LocalizedTextDto> Bodies { get; set; } = new List<LocalizedTextDto>();

    public bool IsOfficial { get; set; }

    public bool CanUpdate { get; set; }

    public bool CanDelete { get; set; }

    public long Revision { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
