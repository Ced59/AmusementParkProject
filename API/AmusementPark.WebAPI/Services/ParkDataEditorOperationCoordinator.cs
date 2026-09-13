namespace AmusementPark.WebAPI.Services;

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


}
