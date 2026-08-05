using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.SocialPublishing.Contracts;

namespace AmusementPark.Application.Features.SocialPublishing.Queries;

public sealed record GetSocialPublishingOverviewQuery(int Limit = 25) : IQuery<SocialPublishingOverview>;
