using System;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class ImagesHttpMappersTests
{
    [Fact]
    public void ToHttp_WhenImageBelongsToParkItem_ShouldExposeParkItemOwnerAndCategory()
    {
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.ParkItem,
            OwnerType = ImageOwnerType.ParkItem,
            OwnerId = "item-1",
        };

        ImageDto dto = image.ToHttp();

        Assert.Equal(ImageCategoryDto.PARK_ITEM, dto.Category);
        Assert.Equal(ImageOwnerTypeDto.PARK_ITEM, dto.OwnerType);
    }

    [Fact]
    public void TryParse_WhenLegacyAttractionImageValuesAreProvided_ShouldMapToParkItem()
    {
        bool categoryParsed = ImagesHttpMappers.TryParseImageCategoryDto("ATTRACTION", out ImageCategoryDto category);
        bool ownerTypeParsed = ImagesHttpMappers.TryParseImageOwnerTypeDto("ATTRACTION", out ImageOwnerTypeDto ownerType);

        Assert.True(categoryParsed);
        Assert.True(ownerTypeParsed);
        Assert.Equal(ImageCategory.ParkItem, category.ToDomain());
        Assert.Equal(ImageOwnerType.ParkItem, ownerType.ToDomain());
    }

    [Fact]
    public void ToHttp_WhenImageHasExifMetadata_ShouldExposePublicMetadata()
    {
        DateTime takenOnUtc = new DateTime(2021, 8, 14, 9, 30, 0, DateTimeKind.Utc);
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Description = "Main entrance",
            IsCurrent = true,
            IsPublished = true,
            Width = 1600,
            Height = 900,
            SizeInBytes = 123456,
            OriginalFileName = "entrance.jpg",
            ContentType = "image/jpeg",
            GeoLocation = new GeoPoint(50.637, 3.063),
            ExifMetadata = new ImageExifMetadata
            {
                CameraMaker = "Canon",
                CameraModel = "EOS",
                TakenOnUtc = takenOnUtc,
                Orientation = "Horizontal",
                FocalLength = 35,
                Aperture = 2.8,
                ExposureTime = 0.008,
                Iso = 200,
            },
            AltTexts = [new LocalizedText("fr", "Entree du parc")],
            Captions = [new LocalizedText("fr", "Vue de l'entree")],
            Credits = [new LocalizedText("fr", "Ced59")],
            TagIds = ["entrance"],
            CreatedAtUtc = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        };

        ImageDto dto = image.ToHttp();

        Assert.NotNull(dto.GeoLocation);
        Assert.Equal(50.637, dto.GeoLocation.Latitude);
        Assert.Equal(3.063, dto.GeoLocation.Longitude);
        Assert.NotNull(dto.ExifMetadata);
        Assert.Equal("Canon", dto.ExifMetadata.CameraMaker);
        Assert.Equal("EOS", dto.ExifMetadata.CameraModel);
        Assert.Equal(takenOnUtc, dto.ExifMetadata.TakenOnUtc);
        Assert.Equal("Horizontal", dto.ExifMetadata.Orientation);
        Assert.Equal(35, dto.ExifMetadata.FocalLength);
        Assert.Equal(2.8, dto.ExifMetadata.Aperture);
        Assert.Equal(0.008, dto.ExifMetadata.ExposureTime);
        Assert.Equal(200, dto.ExifMetadata.Iso);
    }
}
