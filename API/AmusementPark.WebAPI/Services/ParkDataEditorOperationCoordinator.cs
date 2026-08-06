namespace AmusementPark.WebAPI.Services;

public enum ParkDataEditorOperationKind
{
    Read,
    ResourceIntensive,
}

public sealed class ParkDataEditorActiveRequestSnapshot
{
    public string OperationId { get; init; } = string.Empty;

    public ParkDataEditorOperationKind Kind { get; init; }

    public string Method { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public DateTime StartedAtUtc { get; init; }

    public bool InitiatedByCurrentClient { get; init; }
}

public sealed class ParkDataEditorOperationCoordinationSnapshot
{
    public DateTime ServerTimeUtc { get; init; }

    public bool IsBusy { get; init; }

    public bool HasActiveExport { get; init; }

    public bool CanStartResourceIntensiveOperation { get; init; }

    public int ActiveRequestCount { get; init; }

    public int ActiveExportCount { get; init; }

    public int MaxConcurrentRequests { get; init; }

    public int MaxConcurrentResourceIntensiveOperations { get; init; }

    public int RecommendedPollIntervalSeconds { get; init; }

    public int RetryAfterSeconds { get; init; }

    public IReadOnlyCollection<ParkDataEditorActiveRequestSnapshot> ActiveRequests { get; init; } =
        Array.Empty<ParkDataEditorActiveRequestSnapshot>();
}

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

public sealed class ParkDataEditorOperationLease : IDisposable
{
    private readonly Action<string> release;
    private int isDisposed;

    internal ParkDataEditorOperationLease(string operationId, Action<string> release)
    {
        this.OperationId = operationId;
        this.release = release;
    }

    public string OperationId { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.isDisposed, 1) == 0)
        {
            this.release(this.OperationId);
        }
    }
}

public sealed class ParkDataEditorOperationCoordinator : IParkDataEditorOperationCoordinator
{
    public const int ConcurrentRequestLimit = 2;
    public const int ConcurrentResourceIntensiveOperationLimit = 1;
    public const int PollIntervalSeconds = 5;
    public const int BusyRetryAfterSeconds = 5;

    private readonly object syncRoot = new object();
    private readonly Dictionary<string, ActiveOperationState> activeRequests =
        new Dictionary<string, ActiveOperationState>(StringComparer.Ordinal);
    private readonly Dictionary<string, ActiveOperationState> activeExports =
        new Dictionary<string, ActiveOperationState>(StringComparer.Ordinal);

    public int RetryAfterSeconds => BusyRetryAfterSeconds;

    public ParkDataEditorOperationLease? TryBeginRequest(
        string clientId,
        ParkDataEditorOperationKind kind,
        string method,
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        lock (this.syncRoot)
        {
            if (this.activeRequests.Count >= ConcurrentRequestLimit)
            {
                return null;
            }

            if (kind == ParkDataEditorOperationKind.ResourceIntensive
                && (this.activeExports.Count > 0
                    || this.activeRequests.Values.Any(static operation =>
                        operation.Kind == ParkDataEditorOperationKind.ResourceIntensive)))
            {
                return null;
            }

            string operationId = Guid.NewGuid().ToString("N");
            this.activeRequests.Add(operationId, new ActiveOperationState
            {
                OperationId = operationId,
                ClientId = clientId,
                Kind = kind,
                Method = method,
                Path = path,
                StartedAtUtc = DateTime.UtcNow,
            });
            return new ParkDataEditorOperationLease(operationId, this.ReleaseRequest);
        }
    }

    public ParkDataEditorOperationLease? TryBeginExport(string jobId, string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        lock (this.syncRoot)
        {
            bool hasResourceIntensiveRequest = this.activeRequests.Values.Any(static operation =>
                operation.Kind == ParkDataEditorOperationKind.ResourceIntensive);
            if (this.activeExports.Count >= ConcurrentResourceIntensiveOperationLimit
                || hasResourceIntensiveRequest
                || this.activeRequests.Count >= ConcurrentRequestLimit)
            {
                return null;
            }

            this.activeExports.Add(jobId, new ActiveOperationState
            {
                OperationId = jobId,
                ClientId = clientId,
                Kind = ParkDataEditorOperationKind.ResourceIntensive,
                Method = "BACKGROUND",
                Path = "/admin/park-graph-upserts/bulk/export-jobs",
                StartedAtUtc = DateTime.UtcNow,
            });
            return new ParkDataEditorOperationLease(jobId, this.ReleaseExport);
        }
    }

    public ParkDataEditorOperationCoordinationSnapshot GetSnapshot(string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        lock (this.syncRoot)
        {
            DateTime now = DateTime.UtcNow;
            bool hasResourceIntensiveRequest = this.activeRequests.Values.Any(static operation =>
                operation.Kind == ParkDataEditorOperationKind.ResourceIntensive);
            bool hasActiveExport = this.activeExports.Count > 0;
            List<ParkDataEditorActiveRequestSnapshot> requests = this.activeRequests.Values
                .OrderBy(static operation => operation.StartedAtUtc)
                .Select(operation => new ParkDataEditorActiveRequestSnapshot
                {
                    OperationId = operation.OperationId,
                    Kind = operation.Kind,
                    Method = operation.Method,
                    Path = operation.Path,
                    StartedAtUtc = operation.StartedAtUtc,
                    InitiatedByCurrentClient = string.Equals(operation.ClientId, clientId, StringComparison.Ordinal),
                })
                .ToList();

            return new ParkDataEditorOperationCoordinationSnapshot
            {
                ServerTimeUtc = now,
                IsBusy = requests.Count > 0 || hasActiveExport,
                HasActiveExport = hasActiveExport,
                CanStartResourceIntensiveOperation = !hasActiveExport
                    && !hasResourceIntensiveRequest
                    && requests.Count < ConcurrentRequestLimit,
                ActiveRequestCount = requests.Count,
                ActiveExportCount = this.activeExports.Count,
                MaxConcurrentRequests = ConcurrentRequestLimit,
                MaxConcurrentResourceIntensiveOperations = ConcurrentResourceIntensiveOperationLimit,
                RecommendedPollIntervalSeconds = PollIntervalSeconds,
                RetryAfterSeconds = BusyRetryAfterSeconds,
                ActiveRequests = requests,
            };
        }
    }

    private void ReleaseRequest(string operationId)
    {
        lock (this.syncRoot)
        {
            this.activeRequests.Remove(operationId);
        }
    }

    private void ReleaseExport(string operationId)
    {
        lock (this.syncRoot)
        {
            this.activeExports.Remove(operationId);
        }
    }

    private sealed class ActiveOperationState
    {
        public string OperationId { get; init; } = string.Empty;

        public string ClientId { get; init; } = string.Empty;

        public ParkDataEditorOperationKind Kind { get; init; }

        public string Method { get; init; } = string.Empty;

        public string Path { get; init; } = string.Empty;

        public DateTime StartedAtUtc { get; init; }
    }
}
