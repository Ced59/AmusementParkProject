using AmusementPark.Core.Domain.Videos;
using AmusementPark.WebAPI.Contracts.Videos;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class VideosHttpMappersTests
{
    [Fact]
    public void ToHttp_WhenVideoBelongsToParkItem_ShouldExposeParkItemOwner()
    {
        Video video = new Video
        {
            Id = "video-1",
            OwnerType = VideoOwnerType.ParkItem,
            OwnerId = "item-1",
        };

        VideoDto dto = video.ToHttp();

        Assert.Equal(VideoOwnerTypeDto.PARK_ITEM, dto.OwnerType);
    }

    [Fact]
    public void ToDomain_WhenParkItemOwnerIsProvided_ShouldMapToParkItem()
    {
        Assert.Equal(VideoOwnerType.ParkItem, VideoOwnerTypeDto.PARK_ITEM.ToDomain());
    }
}
