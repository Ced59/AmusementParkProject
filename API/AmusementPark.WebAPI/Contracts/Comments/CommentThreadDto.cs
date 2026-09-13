using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Contracts.Comments;

public sealed class CommentThreadDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public List<CommentDto> Comments { get; set; } = new List<CommentDto>();
}
