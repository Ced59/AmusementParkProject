using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

public sealed class BulkParkGraphUpsertRequestDto
{
    public bool CreateIfMissing { get; set; }

    public bool ReplaceCollections { get; set; }

    public JsonElement Document { get; set; }
}
