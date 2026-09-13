using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Contracts.Comments;

public sealed class CommentSummaryDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public long CommentCount { get; set; }

    public string LanguageCode { get; set; } = string.Empty;

    public long LanguageCommentCount { get; set; }

    public CommentDto? OfficialComment { get; set; }
}
