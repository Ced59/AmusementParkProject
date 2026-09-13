using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.WebAPI.Services;

public sealed class BulkParkGraphExportJobSnapshot
{
    public string JobId { get; init; } = string.Empty;

    public BulkParkGraphExportJobStatus Status { get; init; }

    public int ProgressPercentage { get; init; }

    public string? Message { get; init; }

    public int? ExportedParkCount { get; init; }

    public int? ProcessedParkCount { get; init; }

    public string? FileName { get; init; }

    public long? ContentLength { get; init; }

    public string? DownloadToken { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public DateTime ExpiresAtUtc { get; init; }

    public string? Error { get; init; }

    public string RequestedByClientId { get; init; } = string.Empty;
}
