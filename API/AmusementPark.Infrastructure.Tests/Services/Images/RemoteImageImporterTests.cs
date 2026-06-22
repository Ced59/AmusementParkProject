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

public sealed class RemoteImageImporterTests
{
    private static readonly byte[] PngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    [Fact]
    public async Task ImportAsync_WhenCdnReturnsOctetStreamWithoutExtension_ShouldDetectImageFromContent()
    {
        CapturingHttpMessageHandler httpMessageHandler = new CapturingHttpMessageHandler(PngBytes);
        RemoteImageImporter importer = CreateImporter(httpMessageHandler, out Mock<IImageRepository> imageRepository, out Mock<IImageProcessingPipeline> imageProcessingPipeline, out Mock<IImageBinaryStorage> imageBinaryStorage);

        imageProcessingPipeline
            .Setup(pipeline => pipeline.ExtractMetadataAsync(
                It.Is<ImageUploadRequest>(request =>
                    request.File != null &&
                    request.File.FileName == "image.png" &&
                    request.File.ContentType == "image/png" &&
                    request.WithWatermark),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1,
                Height = 1,
                SizeInBytes = PngBytes.Length,
            });

        imageBinaryStorage
            .Setup(storage => storage.SaveAsync(
                It.Is<string>(path => path.StartsWith("park/", StringComparison.Ordinal)),
                It.Is<FilePayload>(file => file.FileName == "image.png" && file.ContentType == "image/png"),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "park/image.webp", "park/image.jpg" });

        imageRepository
            .Setup(repository => repository.CreateAsync(
                It.Is<ImageUploadRequest>(request =>
                    request.File != null &&
                    request.File.FileName == "image.png" &&
                    request.File.ContentType == "image/png" &&
                    request.WithWatermark &&
                    request.SourceUrl == "http://93.184.216.34/image?id=1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImageUploadRequest request, CancellationToken _) => new Image
            {
                Id = request.ImageId ?? "image-id",
                Category = request.Category,
                OwnerType = request.OwnerType,
                OwnerId = request.OwnerId,
                OriginalFileName = request.File!.FileName,
                ContentType = request.File.ContentType,
                SourceUrl = request.SourceUrl,
            });

        Image? image = await importer.ImportAsync(new RemoteImageImportRequest
        {
            SourceUrl = "http://93.184.216.34/image?id=1",
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            WithWatermark = true,
        }, CancellationToken.None);

        Assert.NotNull(image);
        Assert.Equal("image.png", image.OriginalFileName);
        Assert.Equal("image/png", image.ContentType);
        Assert.NotNull(httpMessageHandler.Request);
        Assert.Contains("Mozilla/5.0", httpMessageHandler.Request!.Headers.UserAgent.ToString(), StringComparison.Ordinal);
        Assert.Equal(new Uri("http://93.184.216.34"), httpMessageHandler.Request.Headers.Referrer);
        Assert.Equal("image", httpMessageHandler.Request.Headers.GetValues("Sec-Fetch-Dest").Single());
        imageProcessingPipeline.VerifyAll();
        imageBinaryStorage.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Theory]
    [InlineData(ImageCategory.Park, true, true)]
    [InlineData(ImageCategory.Park, false, false)]
    [InlineData(ImageCategory.ParkLogo, true, false)]
    [InlineData(ImageCategory.ParkLogo, false, false)]
    [InlineData(ImageCategory.Manufacturer, true, false)]
    [InlineData(ImageCategory.Operator, true, false)]
    [InlineData(ImageCategory.Founder, true, false)]
    public void ShouldApplyWatermark_ShouldNeverApplyToLogoCategories(ImageCategory category, bool requestedWithWatermark, bool expected)
    {
        bool result = RemoteImageImporter.ShouldApplyWatermark(category, requestedWithWatermark);

        Assert.Equal(expected, result);
    }

    private static RemoteImageImporter CreateImporter(
        HttpMessageHandler httpMessageHandler,
        out Mock<IImageRepository> imageRepository,
        out Mock<IImageProcessingPipeline> imageProcessingPipeline,
        out Mock<IImageBinaryStorage> imageBinaryStorage)
    {
        imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageProcessingPipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        HttpClient httpClient = new HttpClient(httpMessageHandler);

        return new RemoteImageImporter(
            new StubHttpClientFactory(httpClient),
            imageRepository.Object,
            imageProcessingPipeline.Object,
            imageBinaryStorage.Object,
            NullLogger<RemoteImageImporter>.Instance);
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

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] content;

        public CapturingHttpMessageHandler(byte[] content)
        {
            this.content = content;
        }

        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Request = request;
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(this.content),
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            return Task.FromResult(response);
        }
    }
}
