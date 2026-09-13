using AmusementPark.WebAPI.Contracts.Common;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.SocialPublishing;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SocialNetworkDto
{
    Facebook = 0,
}
