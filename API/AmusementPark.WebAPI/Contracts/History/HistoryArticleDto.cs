using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.Parks;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoryArticleDto
{
    public HistoryEventDto Event { get; set; } = new HistoryEventDto();

    public ParkDto? Park { get; set; }

    public ParkItemDto? ParkItem { get; set; }

    public ParkDto? ContextPark { get; set; }

    public ImageDto? MainImage { get; set; }
}
