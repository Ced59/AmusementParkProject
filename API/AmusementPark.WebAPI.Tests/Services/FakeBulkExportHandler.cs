using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.WebAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Services;

internal sealed class FakeBulkExportHandler : IQueryHandler<ExportBulkParkGraphJsonQuery, ApplicationResult<ParkGraphJsonExportResult>>
{
    public Task<ApplicationResult<ParkGraphJsonExportResult>> HandleAsync(ExportBulkParkGraphJsonQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (query.OutputStream is null)
        {
            return Task.FromResult(ApplicationResult<ParkGraphJsonExportResult>.Failure(ApplicationErrors.Required("outputStream")));
        }

        using Utf8JsonWriter writer = new Utf8JsonWriter(query.OutputStream);
        writer.WriteStartObject();
        writer.WriteString("documentType", "AmusementParkBulkParkGraphUpsert");
        writer.WriteStartArray("parks");
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return Task.FromResult(ApplicationResult<ParkGraphJsonExportResult>.Success(new ParkGraphJsonExportResult
        {
            FileName = "bulk-test.json",
        }));
    }
}
