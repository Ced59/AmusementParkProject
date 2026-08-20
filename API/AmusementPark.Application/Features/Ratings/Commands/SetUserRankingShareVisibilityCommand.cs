using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Ratings.Commands;

public sealed record SetUserRankingShareVisibilityCommand(
    string UserId,
    bool IsPublic) : ICommand<ApplicationResult<UserRankingShareSettingsResult>>;
