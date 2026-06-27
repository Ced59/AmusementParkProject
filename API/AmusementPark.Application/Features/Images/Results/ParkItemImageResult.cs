using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Images.Results;

public sealed class ParkItemImageResult
{
    public ParkItemImageResult(ParkItem item, Image image)
    {
        this.Item = item;
        this.Image = image;
    }

    public ParkItem Item { get; }

    public Image Image { get; }
}
