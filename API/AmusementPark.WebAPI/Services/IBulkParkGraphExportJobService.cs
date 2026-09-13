using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.WebAPI.Services;

public interface IBulkParkGraphExportJobService
{
    Task<BulkParkGraphExportJobStartResult> TryStartAsync(
        ParkGraphBulkExportRequest request,
        string requestedByUserId,
        string requestedByClientId,
        CancellationToken cancellationToken = default);

    BulkParkGraphExportJobSnapshot? GetSnapshot(string jobId, string requestedByUserId);

    IReadOnlyCollection<BulkParkGraphExportJobSnapshot> GetActiveSnapshots();

    BulkParkGraphExportDownload? GetDownload(string jobId, string token);
}
