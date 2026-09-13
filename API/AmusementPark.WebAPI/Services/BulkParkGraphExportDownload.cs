using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.WebAPI.Services;

public sealed class BulkParkGraphExportDownload
{
    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/octet-stream";
}
