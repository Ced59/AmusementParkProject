using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;
using Microsoft.AspNetCore.Http;

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
