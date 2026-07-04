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
        BulkParkGraphExportJobService service = new BulkParkGraphExportJobService(
            scopeFactory,
            NullLogger<BulkParkGraphExportJobService>.Instance);

        BulkParkGraphExportJobSnapshot queuedSnapshot = await service.StartAsync(
            new ParkGraphBulkExportRequest
            {
                SelectionMode = ParkGraphBulkParkSelectionMode.Explicit,
                ParkIds = new[] { "park-1" },
                Sections = new[] { ParkGraphExportSection.ParkBasics },
            },
            "admin-user",
            CancellationToken.None);

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

        File.Delete(download.FilePath);
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
