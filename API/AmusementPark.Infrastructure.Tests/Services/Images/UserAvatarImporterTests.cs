using System.Net;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Services.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Images;

public sealed class UserAvatarImporterTests
{
    private static readonly byte[] PngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    [Fact]
    public async Task DownloadAndSaveAsync_ShouldPersistGoogleAvatarWithoutPrivateMetadata()
    {
        HttpClient httpClient = new HttpClient(new AvatarHttpMessageHandler(PngBytes));
        Mock<IImageProcessingPipeline> imageProcessingPipeline =
            new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> imageBinaryStorage =
            new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository =
            new Mock<IImageRepository>(MockBehavior.Strict);
        imageProcessingPipeline
            .Setup(pipeline => pipeline.ExtractMetadataAsync(
                It.Is<ImageUploadRequest>(request =>
                    request.Category == ImageCategory.Avatar
                    && request.OwnerType == ImageOwnerType.User
                    && request.OwnerId == "user-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1,
                Height = 1,
                SizeInBytes = PngBytes.Length,
                GeoLocation = new GeoPointValue(50, 3),
                ExifMetadata = new ImageExifMetadata { CameraMaker = "Phone" },
            });
        imageRepository
            .Setup(repository => repository.CreateAsync(
                It.Is<ImageUploadRequest>(request =>
                    request.OwnerId == "user-1"
                    && request.GeoLocation == null
                    && request.ExifMetadata == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = "avatar-1",
                Category = ImageCategory.Avatar,
                OwnerType = ImageOwnerType.User,
                OwnerId = "user-1",
                Path = "avatar/avatar-1",
            });
        imageBinaryStorage
            .Setup(storage => storage.SaveWithoutMetadataAsync(
                "avatar/avatar-1",
                It.IsAny<AmusementPark.Application.Common.Contracts.FilePayload>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "avatar/avatar-1.webp", "avatar/avatar-1.jpg" });
        imageRepository
            .Setup(repository => repository.SetCurrentAsync(
                "avatar-1",
                ImageOwnerType.User,
                "user-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = "avatar-1",
                Category = ImageCategory.Avatar,
                OwnerType = ImageOwnerType.User,
                OwnerId = "user-1",
                Path = "avatar/avatar-1",
                IsCurrent = true,
            });
        UserAvatarImporter importer = new UserAvatarImporter(
            new StubHttpClientFactory(httpClient),
            imageProcessingPipeline.Object,
            imageBinaryStorage.Object,
            imageRepository.Object,
            NullLogger<UserAvatarImporter>.Instance);

        string avatarUrl = await importer.DownloadAndSaveAsync(
            "https://accounts.google.test/avatar.png",
            "user-1",
            CancellationToken.None);

        Assert.Equal("/images/avatar-1", avatarUrl);
        imageProcessingPipeline.VerifyAll();
        imageBinaryStorage.VerifyAll();
        imageRepository.VerifyAll();
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient httpClient;

        public StubHttpClientFactory(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            return this.httpClient;
        }
    }

    private sealed class AvatarHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] content;

        public AvatarHttpMessageHandler(byte[] content)
        {
            this.content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(this.content),
            };
            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            return Task.FromResult(response);
        }
    }
}
