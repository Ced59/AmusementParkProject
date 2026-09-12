using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Contracts;

public enum SocialPublicationTargetKind
{
    Park = 0,
    ParkItem = 1,
    Video = 2,
    Page = 3,
    StandaloneAttraction = 4,
}

