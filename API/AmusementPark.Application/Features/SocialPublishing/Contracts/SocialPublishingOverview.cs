using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Contracts;

public sealed record SocialPublishingOverview(
    IReadOnlyCollection<SocialPublisherDescriptor> Publishers,
    IReadOnlyCollection<SocialPublication> RecentPublications);

