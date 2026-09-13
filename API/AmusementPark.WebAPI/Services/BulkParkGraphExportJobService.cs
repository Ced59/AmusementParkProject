using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.WebAPI.Services;

public sealed class BulkParkGraphExportJobService : IBulkParkGraphExportJobService
{
    private static readonly TimeSpan PendingJobLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan CompletedJobLifetime = TimeSpan.FromMinutes(30);
    private const UnixFileMode PrivateDirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode PrivateFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    private readonly ConcurrentDictionary<string, BulkParkGraphExportJobState> jobs = new ConcurrentDictionary<string, BulkParkGraphExportJobState>(StringComparer.Ordinal);
    private readonly SemaphoreSlim exportSemaphore = new SemaphoreSlim(1, 1);
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IParkDataEditorOperationCoordinator operationCoordinator;
    private readonly ILogger<BulkParkGraphExportJobService> logger;
    private readonly string workDirectory;

    public BulkParkGraphExportJobService(
        IServiceScopeFactory serviceScopeFactory,
        IParkDataEditorOperationCoordinator operationCoordinator,
        ILogger<BulkParkGraphExportJobService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.operationCoordinator = operationCoordinator;
        this.logger = logger;
        this.workDirectory = Path.Combine(Path.GetTempPath(), "amusement-park", "bulk-park-graph-exports");
    }

    public Task<BulkParkGraphExportJobStartResult> TryStartAsync(
        ParkGraphBulkExportRequest request,
        string requestedByUserId,
        string requestedByClientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(requestedByUserId))
        {
            throw new ArgumentException("The requesting user identifier is required.", nameof(requestedByUserId));
        }

        if (string.IsNullOrWhiteSpace(requestedByClientId))
        {
            throw new ArgumentException("The requesting client identifier is required.", nameof(requestedByClientId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        DateTime now = DateTime.UtcNow;
        this.CleanupExpired(now);
        EnsurePrivateDirectory(this.workDirectory);

        string jobId = CreateToken(16);
        ParkDataEditorOperationLease? coordinationLease = this.operationCoordinator.TryBeginExport(
            jobId,
            requestedByClientId);
        if (coordinationLease is null)
        {
            return Task.FromResult(new BulkParkGraphExportJobStartResult
            {
                RetryAfterSeconds = this.operationCoordinator.RetryAfterSeconds,
            });
        }

        BulkParkGraphExportJobState state = new BulkParkGraphExportJobState
        {
            JobId = jobId,
            RequestedByUserId = requestedByUserId,
            RequestedByClientId = requestedByClientId,
            DownloadToken = CreateToken(32),
            Request = CloneRequest(request),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(PendingJobLifetime),
            CoordinationLease = coordinationLease,
        };
        try
        {
            state.FilePath = Path.Combine(this.workDirectory, $"{state.JobId}.json");
            state.Message = "Export JSON bulk en file d'attente.";
            this.jobs[state.JobId] = state;

            _ = Task.Run(() => this.ProcessAsync(state.JobId, CancellationToken.None), CancellationToken.None);
            return Task.FromResult(new BulkParkGraphExportJobStartResult
            {
                Snapshot = CreateSnapshot(state, false),
                RetryAfterSeconds = this.operationCoordinator.RetryAfterSeconds,
            });
        }
        catch
        {
            coordinationLease.Dispose();
            throw;
        }
    }

    public BulkParkGraphExportJobSnapshot? GetSnapshot(string jobId, string requestedByUserId)
    {
        if (string.IsNullOrWhiteSpace(jobId) || string.IsNullOrWhiteSpace(requestedByUserId))
        {
            return null;
        }

        DateTime now = DateTime.UtcNow;
        this.CleanupExpired(now);
        if (!this.jobs.TryGetValue(jobId, out BulkParkGraphExportJobState? state))
        {
            return null;
        }

        if (!string.Equals(state.RequestedByUserId, requestedByUserId, StringComparison.Ordinal))
        {
            return null;
        }

        return CreateSnapshot(state, true);
    }

    public IReadOnlyCollection<BulkParkGraphExportJobSnapshot> GetActiveSnapshots()
    {
        this.CleanupExpired(DateTime.UtcNow);
        return this.jobs.Values
            .Select(static state => CreateSnapshot(state, false))
            .Where(static snapshot => snapshot.Status == BulkParkGraphExportJobStatus.Queued
                || snapshot.Status == BulkParkGraphExportJobStatus.Running)
            .OrderBy(static snapshot => snapshot.CreatedAtUtc)
            .ToList();
    }

    public BulkParkGraphExportDownload? GetDownload(string jobId, string token)
    {
        if (string.IsNullOrWhiteSpace(jobId) || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        DateTime now = DateTime.UtcNow;
        this.CleanupExpired(now);
        if (!this.jobs.TryGetValue(jobId, out BulkParkGraphExportJobState? state))
        {
            return null;
        }

        lock (state.SyncRoot)
        {
            if (state.Status != BulkParkGraphExportJobStatus.Completed)
            {
                return null;
            }

            if (!TokenEquals(state.DownloadToken, token))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(state.FilePath) || !File.Exists(state.FilePath))
            {
                return null;
            }

            return new BulkParkGraphExportDownload
            {
                FilePath = state.FilePath,
                FileName = state.FileName ?? "bulk-park-graph-export.json",
                ContentType = "application/octet-stream",
            };
        }
    }

    private async Task ProcessAsync(string jobId, CancellationToken cancellationToken)
    {
        if (!this.jobs.TryGetValue(jobId, out BulkParkGraphExportJobState? state))
        {
            return;
        }

        await this.exportSemaphore.WaitAsync(cancellationToken);
        try
        {
            this.MarkRunning(state);
            using IServiceScope scope = this.serviceScopeFactory.CreateScope();
            IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>> handler =
                scope.ServiceProvider.GetRequiredService<IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>>();
            InlineProgress<ParkGraphJsonExportProgress> progress = new InlineProgress<ParkGraphJsonExportProgress>(value => this.UpdateProgress(state, value));
            ApplicationResult<ParkGraphJsonExportResult> result;
            await using (FileStream output = new FileStream(
                state.FilePath,
                CreatePrivateFileStreamOptions()))
            {
                EnsurePrivateFile(state.FilePath);
                result = await handler.HandleAsync(
                    new ExportBulkParkGraphJsonQuery(state.Request, progress, output),
                    cancellationToken);
            }

            if (!result.IsSuccess || result.Value is null)
            {
                string errorMessage = result.Errors.FirstOrDefault()?.Message ?? "L'export JSON bulk a échoué.";
                TryDeleteFile(state.FilePath);
                this.MarkFailed(state, errorMessage);
                return;
            }

            FileInfo fileInfo = new FileInfo(state.FilePath);
            this.MarkCompleted(state, result.Value.FileName, fileInfo.Length);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryDeleteFile(state.FilePath);
            this.MarkFailed(state, "L'export JSON bulk a été interrompu.");
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Bulk park graph export job {JobId} failed.", jobId);
            TryDeleteFile(state.FilePath);
            this.MarkFailed(state, "L'export JSON bulk a échoué.");
        }
        finally
        {
            state.CoordinationLease?.Dispose();
            state.CoordinationLease = null;
            this.exportSemaphore.Release();
        }
    }

    private void MarkRunning(BulkParkGraphExportJobState state)
    {
        DateTime now = DateTime.UtcNow;
        lock (state.SyncRoot)
        {
            state.Status = BulkParkGraphExportJobStatus.Running;
            state.StartedAtUtc = now;
            state.ProgressPercentage = Math.Max(state.ProgressPercentage, 1);
            state.Message = "Export JSON bulk en cours.";
        }
    }

    private void UpdateProgress(BulkParkGraphExportJobState state, ParkGraphJsonExportProgress progress)
    {
        lock (state.SyncRoot)
        {
            if (state.Status != BulkParkGraphExportJobStatus.Running)
            {
                return;
            }

            state.ProgressPercentage = Math.Max(state.ProgressPercentage, Math.Max(0, Math.Min(99, progress.ProgressPercentage)));
            state.Message = progress.Message ?? state.Message;
            state.ExportedParkCount = progress.ExportedParkCount ?? state.ExportedParkCount;
            state.ProcessedParkCount = progress.ProcessedParkCount ?? state.ProcessedParkCount;
        }
    }

    private void MarkCompleted(BulkParkGraphExportJobState state, string fileName, long contentLength)
    {
        DateTime now = DateTime.UtcNow;
        lock (state.SyncRoot)
        {
            state.Status = BulkParkGraphExportJobStatus.Completed;
            state.ProgressPercentage = 100;
            state.Message = "JSON bulk prêt.";
            state.FileName = fileName;
            state.ContentLength = contentLength;
            state.CompletedAtUtc = now;
            state.ExpiresAtUtc = now.Add(CompletedJobLifetime);
            state.Error = null;
        }
    }

    private void MarkFailed(BulkParkGraphExportJobState state, string errorMessage)
    {
        DateTime now = DateTime.UtcNow;
        lock (state.SyncRoot)
        {
            state.Status = BulkParkGraphExportJobStatus.Failed;
            state.Message = "Export JSON bulk échoué.";
            state.CompletedAtUtc = now;
            state.ExpiresAtUtc = now.Add(CompletedJobLifetime);
            state.Error = errorMessage;
        }
    }

    private void CleanupExpired(DateTime now)
    {
        foreach (KeyValuePair<string, BulkParkGraphExportJobState> pair in this.jobs)
        {
            BulkParkGraphExportJobState state = pair.Value;
            string? filePath = null;
            bool shouldRemove = false;

            lock (state.SyncRoot)
            {
                if ((state.Status == BulkParkGraphExportJobStatus.Completed || state.Status == BulkParkGraphExportJobStatus.Failed || state.Status == BulkParkGraphExportJobStatus.Expired)
                    && now >= state.ExpiresAtUtc)
                {
                    state.Status = BulkParkGraphExportJobStatus.Expired;
                    filePath = state.FilePath;
                    shouldRemove = true;
                }
            }

            if (!shouldRemove)
            {
                continue;
            }

            this.jobs.TryRemove(pair.Key, out _);
            TryDeleteFile(filePath);
        }
    }

    private static BulkParkGraphExportJobSnapshot CreateSnapshot(BulkParkGraphExportJobState state, bool includeDownloadToken)
    {
        lock (state.SyncRoot)
        {
            return new BulkParkGraphExportJobSnapshot
            {
                JobId = state.JobId,
                Status = state.Status,
                ProgressPercentage = state.ProgressPercentage,
                Message = state.Message,
                ExportedParkCount = state.ExportedParkCount,
                ProcessedParkCount = state.ProcessedParkCount,
                FileName = state.FileName,
                ContentLength = state.ContentLength,
                DownloadToken = includeDownloadToken && state.Status == BulkParkGraphExportJobStatus.Completed ? state.DownloadToken : null,
                CreatedAtUtc = state.CreatedAtUtc,
                StartedAtUtc = state.StartedAtUtc,
                CompletedAtUtc = state.CompletedAtUtc,
                ExpiresAtUtc = state.ExpiresAtUtc,
                Error = state.Error,
                RequestedByClientId = state.RequestedByClientId,
            };
        }
    }

    private static ParkGraphBulkExportRequest CloneRequest(ParkGraphBulkExportRequest request)
    {
        return new ParkGraphBulkExportRequest
        {
            SelectionMode = request.SelectionMode,
            ParkIds = request.ParkIds.ToList(),
            SearchTerm = request.SearchTerm,
            IsVisible = request.IsVisible,
            AdminReviewStatus = request.AdminReviewStatus,
            Type = request.Type,
            AudienceClassificationFilter = request.AudienceClassificationFilter,
            CountryCode = request.CountryCode,
            HasValidCoordinates = request.HasValidCoordinates,
            ClosedFilter = request.ClosedFilter,
            OpeningHoursFilter = request.OpeningHoursFilter,
            SortField = request.SortField,
            SortDescending = request.SortDescending,
            Sections = request.Sections.ToList(),
        };
    }

    private static void EnsurePrivateDirectory(string directoryPath)
    {
        DirectoryInfo directory = !OperatingSystem.IsWindows()
            ? Directory.CreateDirectory(directoryPath, PrivateDirectoryMode)
            : Directory.CreateDirectory(directoryPath);

        if (!OperatingSystem.IsWindows())
        {
            directory.UnixFileMode = PrivateDirectoryMode;
        }
    }

    private static FileStreamOptions CreatePrivateFileStreamOptions()
    {
        FileStreamOptions options = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.Read,
            BufferSize = 65536,
            Options = FileOptions.Asynchronous,
        };

        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = PrivateFileMode;
        }

        return options;
    }

    private static void EnsurePrivateFile(string filePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(filePath, PrivateFileMode);
        }
    }

    private static string CreateToken(int byteCount)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool TokenEquals(string expectedToken, string providedToken)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedToken);
        byte[] providedBytes = Encoding.UTF8.GetBytes(providedToken);
        if (expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private static void TryDeleteFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }




}
