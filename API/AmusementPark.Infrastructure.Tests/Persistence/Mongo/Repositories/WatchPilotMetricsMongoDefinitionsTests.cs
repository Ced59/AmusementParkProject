using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using AmusementPark.Application.Features.Watchlists.Services;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class WatchPilotMetricsMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_BoundsAggregateMetricRetention()
    {
        IReadOnlyCollection<CreateIndexModel<WatchPilotDailyMetricsDocument>> indexes =
            WatchPilotMetricsMongoDefinitions.BuildIndexes();

        CreateIndexModel<WatchPilotDailyMetricsDocument> retention = Assert.Single(indexes);
        Assert.Equal("ttl_watch_pilot_metrics", retention.Options.Name);
        Assert.Equal(TimeSpan.Zero, retention.Options.ExpireAfter);
    }

    [Fact]
    public void MonitoredQueueKinds_IncludesInitialAndCorrectionDeliveryWork()
    {
        Assert.Contains(
            FactualNotificationDistributionJob.Kind,
            WatchPilotMetricsRepository.MonitoredQueueKinds);
        Assert.Contains(
            FactualNotificationCorrectionJob.Kind,
            WatchPilotMetricsRepository.MonitoredQueueKinds);
        Assert.Equal(4, WatchPilotMetricsRepository.MonitoredQueueKinds.Count);
    }

    [Fact]
    public void MisleadingReportFieldName_MatchesThePersistedBsonContract()
    {
        Assert.Equal(
            "misleadingReportedAt",
            UserNotificationDocument.MisleadingReportedAtFieldName);
    }
}
