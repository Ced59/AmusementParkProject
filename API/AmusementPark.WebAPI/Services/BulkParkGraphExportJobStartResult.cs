using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.WebAPI.Services;

public sealed class BulkParkGraphExportJobStartResult
{
    public BulkParkGraphExportJobSnapshot? Snapshot { get; init; }

    public int RetryAfterSeconds { get; init; }

    public bool IsAccepted => this.Snapshot is not null;
}
