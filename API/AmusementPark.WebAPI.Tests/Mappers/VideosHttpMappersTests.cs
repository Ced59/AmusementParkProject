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
            LanguageCodes = new List<string> { "fr" },
            ExternalMetadata = new VideoExternalMetadata
            {
                ProviderViewCount = 123456,
            },
        };

        VideoDto dto = video.ToHttp();

        Assert.Equal(VideoOwnerTypeDto.PARK_ITEM, dto.OwnerType);
        Assert.Equal(new[] { "fr" }, dto.LanguageCodes);
        Assert.Equal(123456L, dto.ExternalMetadata.ProviderViewCount);
    }

    [Fact]
    public void ToDomain_WhenParkItemOwnerIsProvided_ShouldMapToParkItem()
    {
        Assert.Equal(VideoOwnerType.ParkItem, VideoOwnerTypeDto.PARK_ITEM.ToDomain());
    }

    [Fact]
    public void ToApplication_WhenLanguageCodesAreProvided_ShouldMapThemToWriteModel()
    {
        VideoWriteDto dto = new VideoWriteDto
        {
            OriginalUrl = "https://www.youtube.com/watch?v=abcdefghijk",
            OwnerType = VideoOwnerTypeDto.PARK,
            OwnerId = "park-1",
            Type = VideoTypeDto.ON_RIDE,
            LanguageCodes = new List<string> { "fr" },
        };

        AmusementPark.Application.Features.Videos.Contracts.VideoWriteModel model = dto.ToApplication();

        Assert.Equal(new[] { "fr" }, model.LanguageCodes);
    }
}
