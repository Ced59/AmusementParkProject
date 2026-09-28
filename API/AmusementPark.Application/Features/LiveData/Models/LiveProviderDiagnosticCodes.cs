namespace AmusementPark.Application.Features.LiveData.Models;

public static class LiveProviderDiagnosticCodes
{
    public const string EmptyResponse = "live-provider.empty-response";

    public const string InvalidObservation = "live-provider.invalid-observation";

    public const string UnknownStatus = "live-provider.unknown-status";

    public const string UnknownQueueState = "live-provider.unknown-queue-state";

    public const string UnknownQueueKind = "live-provider.unknown-queue-kind";

    public const string InvalidQueueValue = "live-provider.invalid-queue-value";

    public const string StatusQueueConflict = "live-provider.status-queue-conflict";

    public const string UnsupportedEntityType = "live-provider.unsupported-entity-type";
}
