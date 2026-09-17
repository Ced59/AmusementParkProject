namespace AmusementPark.Application.Features.Watchlists.Services;

public static class FactualNotificationCorrectionJob
{
    public const string Kind = "watch.distribute-factual-correction";

    public const int PayloadVersion = 1;

    public const int NotificationBatchSize = 100;

    public static string ReceiptId(string eventId, long eventVersion)
    {
        return $"terminal:{eventId.Trim()}:{eventVersion}";
    }
}
