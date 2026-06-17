using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialShare.Contracts;

namespace AmusementPark.Application.Features.SocialShare.Queries;

public sealed record GetSocialShareStatsQuery(SocialShareStatsCriteria Criteria)
    : IQuery<ApplicationResult<SocialShareStatsResult>>;
