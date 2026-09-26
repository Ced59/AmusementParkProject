using AmusementPark.Infrastructure.Persistence.Mongo.Migrations;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Migrations;

public sealed class HistoricalLegacyMigrationIdentityTests
{
    [Fact]
    public void CreateGuid_ShouldBeStableNamespacedAndResourceSpecific()
    {
        Guid first = HistoricalLegacyMigrationIdentity.CreateGuid(
            HistoricalLegacyHistoryReplacementMigration.MigrationId,
            "fact",
            "legacy-1");
        Guid repeated = HistoricalLegacyMigrationIdentity.CreateGuid(
            HistoricalLegacyHistoryReplacementMigration.MigrationId,
            "fact",
            "legacy-1");
        Guid source = HistoricalLegacyMigrationIdentity.CreateGuid(
            HistoricalLegacyHistoryReplacementMigration.MigrationId,
            "source",
            "legacy-1");

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, source);
        Assert.NotEqual(Guid.Empty, first);
    }

    [Fact]
    public void CreateGuid_WhenSourceIndexChanges_ShouldCreateAnotherIdentity()
    {
        Guid first = HistoricalLegacyMigrationIdentity.CreateGuid(
            HistoricalLegacyHistoryReplacementMigration.MigrationId,
            "source",
            "legacy-1",
            0);
        Guid second = HistoricalLegacyMigrationIdentity.CreateGuid(
            HistoricalLegacyHistoryReplacementMigration.MigrationId,
            "source",
            "legacy-1",
            1);

        Assert.NotEqual(first, second);
    }
}
