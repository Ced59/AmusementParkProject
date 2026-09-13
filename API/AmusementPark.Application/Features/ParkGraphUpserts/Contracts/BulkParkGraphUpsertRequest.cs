using System.Text.Json;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class BulkParkGraphUpsertRequest
{
    public bool CreateIfMissing { get; init; }

    public bool ReplaceCollections { get; init; }

    public JsonElement Document { get; init; }

    public string RawJson { get; init; } = string.Empty;
}
