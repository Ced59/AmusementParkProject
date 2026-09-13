using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.Parks;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistorySourceReferenceDto
{
    public string? Label { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? AccessedAt { get; set; }
}
