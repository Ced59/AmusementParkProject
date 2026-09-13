using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Application.Features.Sharing.Queries;

public sealed record GetProfileComparisonInvitationPreviewQuery(string UserId, string Token)
    : IQuery<ApplicationResult<ProfileComparisonInvitationPreviewResult>>;
