using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Results;

public sealed class ParkItemVideoResult
{
    public ParkItemVideoResult(ParkItem item, Video video)
    {
        this.Item = item;
        this.Video = video;
    }

    public ParkItem Item { get; }

    public Video Video { get; }
}
