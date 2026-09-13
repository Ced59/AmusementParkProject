using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

public sealed class ParkGraphUpsertCountsDto
{
    public int Created { get; set; }

    public int Updated { get; set; }

    public int Deleted { get; set; }

    public int Unchanged { get; set; }

    public int Warnings { get; set; }

    public int Errors { get; set; }
}
