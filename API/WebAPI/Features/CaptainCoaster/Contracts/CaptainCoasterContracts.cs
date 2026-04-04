namespace WebAPI.Features.CaptainCoaster.Contracts
{
    public sealed class CaptainCoasterDataSourceStatusResponse
    {
        public string Source { get; set; } = "CaptainCoaster";
        public bool IsEnabled { get; set; }
        public DateTime? LastSuccessfulImportUtc { get; set; }
        public int TotalSessionsCount { get; set; }
    }

    public sealed class CaptainCoasterSyncSessionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public string CurrentStep { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public int ParksFetched { get; set; }
        public int CoastersFetched { get; set; }
        public int ComparisonResults { get; set; }
        public int AppliedChanges { get; set; }
        public IReadOnlyCollection<CaptainCoasterSyncLogResponse> Logs { get; set; } = Array.Empty<CaptainCoasterSyncLogResponse>();
    }

    public sealed class CaptainCoasterSyncLogResponse
    {
        public DateTime OccurredAtUtc { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Résultat paginé de la comparaison. Contient les compteurs globaux (toujours depuis le back)
    /// pour éviter de charger toutes les lignes côté client.
    /// </summary>
    public sealed class CaptainCoasterComparisonPagedResponse
    {
        public IReadOnlyCollection<CaptainCoasterComparisonResultResponse> Items { get; set; } = Array.Empty<CaptainCoasterComparisonResultResponse>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        /// <summary>Nombre total d'Updated dans la session (indépendant de la page courante).</summary>
        public int SessionUpdatedCount { get; set; }
        /// <summary>Nombre total de MissingLocal dans la session.</summary>
        public int SessionMissingCount { get; set; }
        /// <summary>Nombre total déjà appliqués dans la session.</summary>
        public int SessionAppliedCount { get; set; }
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
        /// <summary>IDs spécifiques à appliquer.</summary>
        public IReadOnlyCollection<string> ComparisonResultIds { get; set; } = Array.Empty<string>();
        /// <summary>Si true, applique tous les résultats non encore appliqués de la session.</summary>
        public bool ApplyAll { get; set; }
        /// <summary>Session ciblée pour ApplyAll (null = dernière session).</summary>
        public string? SessionId { get; set; }
        /// <summary>Filtre optionnel sur EntityType pour ApplyAll.</summary>
        public string? EntityTypeFilter { get; set; }
        /// <summary>Filtre optionnel sur ChangeType pour ApplyAll.</summary>
        public string? ChangeTypeFilter { get; set; }
    }

    public sealed class ApplyCaptainCoasterComparisonResponse
    {
        public int AppliedCount { get; set; }
    }
}
