using AmusementPark.WebAPI.Contracts.Common;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.SocialPublishing;

public sealed class PublishSocialLinkRequestDto
{
    public SocialNetworkDto Network { get; set; } = SocialNetworkDto.Facebook;

    public string? Message { get; set; }

    public string? Url { get; set; }

    public string? PreviewImageId { get; set; }
}
