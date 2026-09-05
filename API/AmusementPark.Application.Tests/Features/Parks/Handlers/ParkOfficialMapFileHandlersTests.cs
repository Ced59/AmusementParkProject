using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class ParkOfficialMapFileHandlersTests
{
    [Theory]
    [InlineData("plan.pdf", "JVBERi0xLjcKbWFw", "pdf", "application/pdf", ParkOfficialMapFormat.Pdf)]
    [InlineData("plan.jpg", "/9j/AA==", "jpg", "image/jpeg", ParkOfficialMapFormat.Image)]
    [InlineData("plan.kml", "PGttbD48L2ttbD4=", "kml", "application/vnd.google-earth.kml+xml", ParkOfficialMapFormat.Other)]
    public async Task Upload_WhenFileSignatureIsSupported_ShouldStoreCanonicalDocumentMetadata(
        string fileName,
        string base64Payload,
        string expectedExtension,
        string expectedContentType,
        ParkOfficialMapFormat expectedFormat)
    {
        Park park = new Park { Id = "park-1", Name = "Test", IsVisible = true };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository.Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkOfficialMapBinaryStorage> storage = new Mock<IParkOfficialMapBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.SaveAsync(
                It.Is<string>(storageKey => IsVersionedStorageKey(storageKey, expectedExtension)),
                It.IsAny<FilePayload>(),
                expectedContentType,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        byte[] bytes = Convert.FromBase64String(base64Payload);
        await using MemoryStream content = new MemoryStream(bytes);
        UploadParkOfficialMapFileCommandHandler handler = new UploadParkOfficialMapFileCommandHandler(
            parkRepository.Object,
            storage.Object);

        ApplicationResult<ParkOfficialMapStoredFile> result = await handler.HandleAsync(
            new UploadParkOfficialMapFileCommand(new ParkOfficialMapFileUploadRequest(
                "park-1",
                "map-2026",
                new FilePayload
                {
                    FileName = fileName,
                    ContentType = "application/octet-stream",
                    Length = bytes.Length,
                    Content = content,
                })));

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedFormat, result.Value!.SuggestedFormat);
        Assert.Equal(expectedContentType, result.Value.ContentType);
        Assert.True(IsVersionedStorageKey(result.Value.StorageKey, expectedExtension));
        parkRepository.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task Get_WhenStoredMapIsHiddenForAnonymousUser_ShouldNotReadBinary()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Test",
            IsVisible = true,
            OfficialMaps = new List<ParkOfficialMap>
            {
                new ParkOfficialMap
                {
                    Id = "map-2026",
                    Year = 2026,
                    Format = ParkOfficialMapFormat.Pdf,
                    StorageKey = "official-maps/park-1/map-2026.pdf",
                    IsVisible = false,
                },
            },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository.Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkOfficialMapBinaryStorage> storage = new Mock<IParkOfficialMapBinaryStorage>(MockBehavior.Strict);
        GetParkOfficialMapFileQueryHandler handler = new GetParkOfficialMapFileQueryHandler(
            parkRepository.Object,
            storage.Object);

        ApplicationResult<ParkOfficialMapBinary> result = await handler.HandleAsync(
            new GetParkOfficialMapFileQuery("park-1", "map-2026"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park.official-map.file-not-found");
        storage.Verify(value => value.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task Get_WhenStoredMapIsPublic_ShouldReturnTheValidatedBinary()
    {
        const string storageKey = "official-maps/park-1/map-2026.0123456789abcdef0123456789abcdef.pdf";
        Park park = new Park
        {
            Id = "park-1",
            Name = "Test",
            IsVisible = true,
            OfficialMaps = new List<ParkOfficialMap>
            {
                new ParkOfficialMap
                {
                    Id = "map-2026",
                    Year = 2026,
                    Format = ParkOfficialMapFormat.Pdf,
                    StorageKey = storageKey,
                    OriginalFileName = "official-map-2026.pdf",
                    ContentType = "application/pdf",
                    IsVisible = true,
                },
            },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository.Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        MemoryStream content = new MemoryStream(new byte[] { 1, 2, 3 });
        Mock<IParkOfficialMapBinaryStorage> storage = new Mock<IParkOfficialMapBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.GetAsync(storageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
        GetParkOfficialMapFileQueryHandler handler = new GetParkOfficialMapFileQueryHandler(
            parkRepository.Object,
            storage.Object);

        ApplicationResult<ParkOfficialMapBinary> result = await handler.HandleAsync(
            new GetParkOfficialMapFileQuery("park-1", "map-2026"));

        Assert.True(result.IsSuccess);
        Assert.Same(content, result.Value!.Content);
        Assert.Equal("application/pdf", result.Value.ContentType);
        Assert.Equal("official-map-2026.pdf", result.Value.FileName);
        Assert.True(result.Value.DisplayInline);
        storage.VerifyAll();
        parkRepository.VerifyAll();
    }

    private static bool IsVersionedStorageKey(string storageKey, string extension)
    {
        const string prefix = "official-maps/park-1/map-2026.";
        string suffix = $".{extension}";
        if (!storageKey.StartsWith(prefix, StringComparison.Ordinal)
            || !storageKey.EndsWith(suffix, StringComparison.Ordinal))
        {
            return false;
        }

        string version = storageKey[prefix.Length..^suffix.Length];
        return version.Length == 32 && version.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    }
}
