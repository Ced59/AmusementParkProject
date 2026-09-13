namespace AmusementPark.WebAPI.Services;

public interface IParkDataEditorOperationCoordinator
{
    int RetryAfterSeconds { get; }

    ParkDataEditorOperationLease? TryBeginRequest(
        string clientId,
        ParkDataEditorOperationKind kind,
        string method,
        string path);

    ParkDataEditorOperationLease? TryBeginExport(string jobId, string clientId);

    ParkDataEditorOperationCoordinationSnapshot GetSnapshot(string clientId);
}
