using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserCollectionEntryMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ProtectsIdentityAndBoundedOwnerSlots()
    {
        IReadOnlyCollection<MongoDB.Driver.CreateIndexModel<UserCollectionEntryDocument>> indexes =
            UserCollectionEntryMongoDefinitions.BuildIndexes();

        Assert.Contains(indexes, index =>
            index.Options.Name == UserCollectionEntryMongoDefinitions.IdentityUniqueIndexName
            && index.Options.Unique == true);
        Assert.Contains(indexes, index =>
            index.Options.Name == UserCollectionEntryMongoDefinitions.OwnerSlotUniqueIndexName
            && index.Options.Unique == true);
        Assert.Contains(indexes, index =>
            index.Options.Name == UserCollectionEntryMongoDefinitions.OwnerUpdatedIndexName
            && index.Options.Unique != true);
    }
}
