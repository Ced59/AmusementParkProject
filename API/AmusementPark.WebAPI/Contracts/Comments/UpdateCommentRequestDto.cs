using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Contracts.Comments;

public sealed class UpdateCommentRequestDto
{
    [Required]
    public List<LocalizedTextDto> Bodies { get; set; } = new List<LocalizedTextDto>();

    public bool IsOfficial { get; set; }

    public long? Revision { get; set; }
}
