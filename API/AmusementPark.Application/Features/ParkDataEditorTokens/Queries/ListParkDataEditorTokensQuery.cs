using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Queries;

public sealed record ListParkDataEditorTokensQuery(
    string UserId) : IQuery<ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>>>;
