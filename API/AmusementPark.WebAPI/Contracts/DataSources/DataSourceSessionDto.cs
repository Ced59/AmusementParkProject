using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceSessionDto
{
    public string SessionId { get; set; } = string.Empty;

    public string SourceKey { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ImportKind { get; set; } = string.Empty;

    public int ProgressPercentage { get; set; }

    public string CurrentStep { get; set; } = string.Empty;

    public string? LastCompletedStep { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool CanResume { get; set; }

    public IReadOnlyCollection<string> AvailableSteps { get; set; } = Array.Empty<string>();

    public DateTime StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DataSourceMetricsDto Metrics { get; set; } = new DataSourceMetricsDto();

    public IReadOnlyCollection<DataSourceLogDto> Logs { get; set; } = Array.Empty<DataSourceLogDto>();
}
