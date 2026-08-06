using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.WebAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Services;

public sealed class BulkParkGraphExportJobServiceTests
{
    [Fact]
    public async Task StartAsync_WhenHandlerCompletes_ShouldStoreDownloadFileInWritableTempDirectory()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>, FakeBulkExportHandler>();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        ParkDataEditorOperationCoordinator coordinator = new ParkDataEditorOperationCoordinator();
        BulkParkGraphExportJobService service = new BulkParkGraphExportJobService(
            scopeFactory,
            coordinator,
            NullLogger<BulkParkGraphExportJobService>.Instance);

        BulkParkGraphExportJobStartResult startResult = await service.TryStartAsync(
            new ParkGraphBulkExportRequest
            {
                SelectionMode = ParkGraphBulkParkSelectionMode.Explicit,
                ParkIds = new[] { "park-1" },
                Sections = new[] { ParkGraphExportSection.ParkBasics },
            },
            "admin-user",
            "token-1",
            CancellationToken.None);
        BulkParkGraphExportJobSnapshot queuedSnapshot = Assert.IsType<BulkParkGraphExportJobSnapshot>(startResult.Snapshot);

        BulkParkGraphExportJobSnapshot completedSnapshot = await WaitForTerminalSnapshotAsync(
            service,
            queuedSnapshot.JobId,
            "admin-user");

        Assert.Equal(BulkParkGraphExportJobStatus.Completed, completedSnapshot.Status);
        Assert.Equal(100, completedSnapshot.ProgressPercentage);
        Assert.NotNull(completedSnapshot.DownloadToken);

        BulkParkGraphExportDownload? download = service.GetDownload(queuedSnapshot.JobId, completedSnapshot.DownloadToken!);
        Assert.NotNull(download);

        string expectedDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "amusement-park", "bulk-park-graph-exports"));
        string actualPath = Path.GetFullPath(download.FilePath);
        Assert.StartsWith(expectedDirectory, actualPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("bulk-test.json", download.FileName);
        Assert.True(File.Exists(download.FilePath));
        Assert.Contains("\"documentType\":\"AmusementParkBulkParkGraphUpsert\"", await File.ReadAllTextAsync(download.FilePath));
        AssertPrivateUnixPermissions(expectedDirectory, download.FilePath);

        File.Delete(download.FilePath);
    }

    [Fact]
    public async Task TryStartAsync_WhenResourceIntensiveOperationIsActive_ShouldRejectWithoutQueuing()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>, FakeBulkExportHandler>();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        ParkDataEditorOperationCoordinator coordinator = new ParkDataEditorOperationCoordinator();
        using ParkDataEditorOperationLease activeOperation = coordinator.TryBeginRequest(
            "token-1",
            ParkDataEditorOperationKind.ResourceIntensive,
            "POST",
            "/admin/park-graph-upserts/preview")!;
        BulkParkGraphExportJobService service = new BulkParkGraphExportJobService(
            scopeFactory,
            coordinator,
            NullLogger<BulkParkGraphExportJobService>.Instance);

        BulkParkGraphExportJobStartResult result = await service.TryStartAsync(
            new ParkGraphBulkExportRequest(),
            "admin-user",
            "token-2",
            CancellationToken.None);

        Assert.False(result.IsAccepted);
        Assert.Null(result.Snapshot);
        Assert.Equal(ParkDataEditorOperationCoordinator.BusyRetryAfterSeconds, result.RetryAfterSeconds);
        Assert.Empty(service.GetActiveSnapshots());
    }

    private static void AssertPrivateUnixPermissions(string directoryPath, string filePath)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        DirectoryInfo directory = new DirectoryInfo(directoryPath);
        FileInfo file = new FileInfo(filePath);

        Assert.Equal(
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
            directory.UnixFileMode);
        Assert.Equal(
            UnixFileMode.UserRead | UnixFileMode.UserWrite,
            file.UnixFileMode);
    }

    private static async Task<BulkParkGraphExportJobSnapshot> WaitForTerminalSnapshotAsync(
        BulkParkGraphExportJobService service,
        string jobId,
        string requestedByUserId)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            BulkParkGraphExportJobSnapshot? snapshot = service.GetSnapshot(jobId, requestedByUserId);
            if (snapshot is not null
                && (snapshot.Status == BulkParkGraphExportJobStatus.Completed
                    || snapshot.Status == BulkParkGraphExportJobStatus.Failed
                    || snapshot.Status == BulkParkGraphExportJobStatus.Expired))
            {
                return snapshot;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("The bulk export job did not reach a terminal state.");
    }

    private sealed class FakeBulkExportHandler : IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>
    {
        public Task<ApplicationResult<ParkGraphJsonExportResult>> HandleAsync(ExportBulkParkGraphJsonQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (query.OutputStream is null)
            {
                return Task.FromResult(ApplicationResult<ParkGraphJsonExportResult>.Failure(ApplicationErrors.Required("outputStream")));
            }

            using Utf8JsonWriter writer = new Utf8JsonWriter(query.OutputStream);
            writer.WriteStartObject();
            writer.WriteString("documentType", "AmusementParkBulkParkGraphUpsert");
            writer.WriteStartArray("parks");
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();

            return Task.FromResult(ApplicationResult<ParkGraphJsonExportResult>.Success(new ParkGraphJsonExportResult
            {
                FileName = "bulk-test.json",
            }));
        }
    }
}
