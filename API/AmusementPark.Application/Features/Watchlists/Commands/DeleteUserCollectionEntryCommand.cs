using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Models;

namespace AmusementPark.Application.Features.Watchlists.Commands;

public sealed record DeleteUserCollectionEntryCommand(
    string UserId,
    UserCollectionTargetInput Target)
    : ICommand<ApplicationResult>;
