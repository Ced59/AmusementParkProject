using AmusementPark.WebAPI.Contracts.Common;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.SocialPublishing;

public sealed class ParkSocialPreviewPublicationDto
{
    public string ParkId { get; set; } = string.Empty;
}
