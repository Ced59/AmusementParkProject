using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Application.Features.Sharing.Queries;

public sealed record GetYearRecapShareSelectionQuery(
    string OwnerUserId,
    int Year)
    : IQuery<ApplicationResult<YearRecapShareSelectionResult>>;
