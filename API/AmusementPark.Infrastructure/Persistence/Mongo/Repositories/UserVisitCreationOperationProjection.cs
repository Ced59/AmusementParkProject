namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class UserVisitCreationOperationProjection
{
    public string Id { get; init; } = string.Empty;

    public string? CreationOperationKeyHash { get; init; }
}
