namespace AmusementPark.Application.Features.FactualEvents;

public static class FactualChangeMaterializationErrorCodes
{
    public const string InvalidPayload = "watch.materialization.invalid-payload";
    public const string SourceMissing = "watch.materialization.source-missing";
    public const string SourceConflict = "watch.materialization.source-conflict";
    public const string EventConflict = "watch.materialization.event-conflict";
    public const string AcknowledgementConflict = "watch.materialization.acknowledgement-conflict";
}
