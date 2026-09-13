using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Application.Features.Sharing.Commands;

public sealed record RevokeProfileComparisonCommand(string UserId, string ShareId)
    : ICommand<ApplicationResult<ProfileComparisonRevocationResult>>;
