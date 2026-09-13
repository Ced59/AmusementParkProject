using AmusementPark.WebAPI.Contracts.Common;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.SocialPublishing;

public sealed class SocialPublisherDto
{
    public string Network { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public bool IsConfigured { get; set; }

    public string? TargetUrl { get; set; }

    public bool SupportsAutomaticParkAnnouncements { get; set; }
}
