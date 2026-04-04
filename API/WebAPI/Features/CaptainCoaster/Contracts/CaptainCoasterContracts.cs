namespace WebAPI.Features.CaptainCoaster.Contracts
{
    public sealed class AdminDataSourceSummaryResponse
    {
        public string SourceKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string InputMode { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime? LastSuccessfulImportUtc { get; set; }
        public int LastImportedParkCount { get; set; }
        public int LastImportedCoasterCount { get; set; }
        public int LastComparisonResultCount { get; set; }
    }

    public sealed class CaptainCoasterSettingsResponse
    {
        public string SourceKey { get; set; } = "captain-coaster";
        public string DisplayName { get; set; } = "Captain Coaster";
        public string Description { get; set; } = "Import de données JSON Captain Coaster avec staging, analyse et application sélective.";
        public string InputMode { get; set; } = "JsonImport";
        public bool IsEnabled { get; set; }
        public DateTime? LastSuccessfulImportUtc { get; set; }
        public IReadOnlyCollection<string> ExpectedFiles { get; set; } = Array.Empty<string>();
    }

    public sealed class UpdateCaptainCoasterSettingsRequest
    {
        public bool IsEnabled { get; set; } = true;
    }

    public sealed class CaptainCoasterSyncSessionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string SourceKey { get; set; } = "captain-coaster";
        public string Status { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public string CurrentStep { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public int SourceFileCount { get; set; }
        public IReadOnlyCollection<string> SourceFileNames { get; set; } = Array.Empty<string>();
        public int ParksFetched { get; set; }
        public int CoastersFetched { get; set; }
        public int ComparisonResults { get; set; }
        public int AppliedChanges { get; set; }
        public string? ManifestSummary { get; set; }
        public IReadOnlyCollection<CaptainCoasterSyncLogResponse> Logs { get; set; } = Array.Empty<CaptainCoasterSyncLogResponse>();
    }

    public sealed class CaptainCoasterSyncLogResponse
    {
        public DateTime OccurredAtUtc { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public sealed class CaptainCoasterComparisonResultResponse
    {
        public string Id { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? LocalEntityId { get; set; }
        public string? ExternalEntityId { get; set; }
        public string MatchConfidence { get; set; } = string.Empty;
        public bool IsApplied { get; set; }
        public IReadOnlyCollection<CaptainCoasterFieldChangeResponse> Changes { get; set; } = Array.Empty<CaptainCoasterFieldChangeResponse>();
    }

    public sealed class CaptainCoasterFieldChangeResponse
    {
        public string Field { get; set; } = string.Empty;
        public string? LocalValue { get; set; }
        public string? ExternalValue { get; set; }
        public bool IsDifferent { get; set; }
    }

    public sealed class ApplyCaptainCoasterComparisonRequest
    {
        public IReadOnlyCollection<string> ComparisonResultIds { get; set; } = Array.Empty<string>();
    }
}
