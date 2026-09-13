using AmusementPark.WebAPI.Contracts.Common;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.SocialPublishing;

public sealed class SocialPublicationSynchronizationResultDto
{
    public int CheckedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int DeletedCount { get; set; }

    public int FailureCount { get; set; }
}
