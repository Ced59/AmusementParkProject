using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class NotificationDigestMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldProtectOneDigestPerOwnerChannelAndPeriod()
    {
        IReadOnlyCollection<CreateIndexModel<NotificationDigestDocument>> indexes =
            NotificationDigestMongoDefinitions.BuildIndexes();

        CreateIndexModel<NotificationDigestDocument> unique = indexes.Single(
            index => index.Options.Name == "uq_notification_digest_group");
        Assert.Equal("uq_notification_digest_group", unique.Options.Name);
        Assert.True(unique.Options.Unique);
        Assert.Contains(indexes, index => index.Options.Name == "ix_notification_digest_pilot_created");
    }
}
