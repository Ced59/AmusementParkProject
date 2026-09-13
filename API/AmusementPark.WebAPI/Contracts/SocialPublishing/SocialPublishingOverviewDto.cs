using AmusementPark.WebAPI.Contracts.Common;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.SocialPublishing;

public sealed class SocialPublishingOverviewDto
{
    public List<SocialPublisherDto> Publishers { get; set; } = new List<SocialPublisherDto>();

    public List<SocialPublicationDto> RecentPublications { get; set; } = new List<SocialPublicationDto>();
}
