using AmusementPark.WebAPI.Contracts.Common;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.SocialPublishing;

public sealed class SocialPublicationImageOptionDto
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }
}
