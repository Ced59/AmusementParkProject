using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class NotificationEmailPreferenceMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldKeepOneConsentStatePerUser()
    {
        IReadOnlyCollection<CreateIndexModel<NotificationEmailPreferenceDocument>> indexes =
            NotificationEmailPreferenceMongoDefinitions.BuildIndexes();

        CreateIndexModel<NotificationEmailPreferenceDocument> unique = Assert.Single(indexes);
        Assert.Equal("uq_notification_email_preference_user", unique.Options.Name);
        Assert.True(unique.Options.Unique);
    }
}
