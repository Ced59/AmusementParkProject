using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record UserCollectionTargetInput(
    CollectionTargetType TargetType,
    string TargetId,
    UserCollectionKind Kind);
