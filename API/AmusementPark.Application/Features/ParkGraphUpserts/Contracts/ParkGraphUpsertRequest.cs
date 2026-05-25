using System.Text.Json;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

/// <summary>
/// Requête applicative d'upsert partiel d'un graphe de parc.
/// </summary>
public sealed class ParkGraphUpsertRequest
{
    public string? TargetParkId { get; init; }

    public bool CreateIfMissing { get; init; }

    public bool ReplaceCollections { get; init; }

    public JsonElement Document { get; init; }

    public string RawJson { get; init; } = string.Empty;
}
