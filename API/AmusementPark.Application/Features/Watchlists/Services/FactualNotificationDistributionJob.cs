namespace AmusementPark.Application.Features.Watchlists.Services;

public static class FactualNotificationDistributionJob
{
    public const string Kind = "watch.distribute-web-notifications";

    public const int PayloadVersion = 1;

    public const int SubscriptionBatchSize = 100;
}
