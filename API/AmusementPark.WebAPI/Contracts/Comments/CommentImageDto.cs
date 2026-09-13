using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Contracts.Comments;

public sealed class CommentImageDto
{
    public string Id { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public long SizeInBytes { get; set; }

    public string? ContentType { get; set; }
}
