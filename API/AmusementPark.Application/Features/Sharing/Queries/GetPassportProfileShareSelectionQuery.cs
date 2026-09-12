using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Application.Features.Sharing.Queries;

public sealed record GetPassportProfileShareSelectionQuery(string OwnerUserId)
    : IQuery<ApplicationResult<PassportProfileShareSelectionResult>>;
