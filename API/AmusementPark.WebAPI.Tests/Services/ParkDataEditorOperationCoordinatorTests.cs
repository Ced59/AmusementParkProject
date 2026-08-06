using AmusementPark.WebAPI.Services;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Services;

public sealed class ParkDataEditorOperationCoordinatorTests
{
    [Fact]
    public void GetSnapshot_ShouldExposeRequestsFromOtherClientsWithoutTheirIdentifier()
    {
        ParkDataEditorOperationCoordinator coordinator = new ParkDataEditorOperationCoordinator();
        using ParkDataEditorOperationLease lease = coordinator.TryBeginRequest(
            "token-a",
            ParkDataEditorOperationKind.Read,
            "GET",
            "/park-data-editor/parks")!;

        ParkDataEditorOperationCoordinationSnapshot snapshot = coordinator.GetSnapshot("token-b");

        Assert.True(snapshot.IsBusy);
        Assert.Equal(1, snapshot.ActiveRequestCount);
        ParkDataEditorActiveRequestSnapshot request = Assert.Single(snapshot.ActiveRequests);
        Assert.False(request.InitiatedByCurrentClient);
        Assert.Equal("/park-data-editor/parks", request.Path);
        Assert.DoesNotContain("token-a", request.OperationId, StringComparison.Ordinal);
    }

    [Fact]
    public void TryBeginRequest_ShouldAllowAtMostOneResourceIntensiveOperation()
    {
        ParkDataEditorOperationCoordinator coordinator = new ParkDataEditorOperationCoordinator();
        using ParkDataEditorOperationLease first = coordinator.TryBeginRequest(
            "token-a",
            ParkDataEditorOperationKind.ResourceIntensive,
            "POST",
            "/admin/park-graph-upserts/preview")!;

        ParkDataEditorOperationLease? rejected = coordinator.TryBeginRequest(
            "token-b",
            ParkDataEditorOperationKind.ResourceIntensive,
            "POST",
            "/admin/park-graph-upserts/apply");
        ParkDataEditorOperationLease? permittedRead = coordinator.TryBeginRequest(
            "token-b",
            ParkDataEditorOperationKind.Read,
            "GET",
            "/park-data-editor/parks");

        Assert.Null(rejected);
        Assert.NotNull(permittedRead);
        permittedRead.Dispose();
    }

    [Fact]
    public void TryBeginExport_ShouldBlockResourceIntensiveRequestsUntilReleased()
    {
        ParkDataEditorOperationCoordinator coordinator = new ParkDataEditorOperationCoordinator();
        ParkDataEditorOperationLease export = coordinator.TryBeginExport("job-1", "token-a")!;

        ParkDataEditorOperationLease? rejected = coordinator.TryBeginRequest(
            "token-b",
            ParkDataEditorOperationKind.ResourceIntensive,
            "POST",
            "/park-data-editor/images");
        ParkDataEditorOperationCoordinationSnapshot activeSnapshot = coordinator.GetSnapshot("token-b");

        Assert.Null(rejected);
        Assert.True(activeSnapshot.HasActiveExport);
        Assert.False(activeSnapshot.CanStartResourceIntensiveOperation);

        export.Dispose();
        using ParkDataEditorOperationLease admitted = coordinator.TryBeginRequest(
            "token-b",
            ParkDataEditorOperationKind.ResourceIntensive,
            "POST",
            "/park-data-editor/images")!;
        Assert.NotNull(admitted);
    }

    [Fact]
    public void TryBeginRequest_ShouldRejectRequestsBeyondTheGlobalLimit()
    {
        ParkDataEditorOperationCoordinator coordinator = new ParkDataEditorOperationCoordinator();
        using ParkDataEditorOperationLease first = coordinator.TryBeginRequest(
            "token-a",
            ParkDataEditorOperationKind.Read,
            "GET",
            "/park-data-editor/parks")!;
        using ParkDataEditorOperationLease second = coordinator.TryBeginRequest(
            "token-b",
            ParkDataEditorOperationKind.Read,
            "GET",
            "/park-data-editor/parks/park-1/data-completeness")!;

        ParkDataEditorOperationLease? rejected = coordinator.TryBeginRequest(
            "token-c",
            ParkDataEditorOperationKind.Read,
            "GET",
            "/admin/park-graph-upserts/history");

        Assert.Null(rejected);
    }
}
