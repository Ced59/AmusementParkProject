using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.WebAPI.Services;

internal sealed class BulkParkGraphExportJobState
{
    public object SyncRoot { get; } = new object();

    public string JobId { get; init; } = string.Empty;

    public string RequestedByUserId { get; init; } = string.Empty;

    public string RequestedByClientId { get; init; } = string.Empty;

    public string DownloadToken { get; init; } = string.Empty;

    public ParkGraphBulkExportRequest Request { get; init; } = new ParkGraphBulkExportRequest();

    public string FilePath { get; set; } = string.Empty;

    public BulkParkGraphExportJobStatus Status { get; set; } = BulkParkGraphExportJobStatus.Queued;

    public int ProgressPercentage { get; set; }

    public string? Message { get; set; }

    public int? ExportedParkCount { get; set; }

    public int? ProcessedParkCount { get; set; }

    public string? FileName { get; set; }

    public long? ContentLength { get; set; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public string? Error { get; set; }

    public ParkDataEditorOperationLease? CoordinationLease { get; set; }
}
