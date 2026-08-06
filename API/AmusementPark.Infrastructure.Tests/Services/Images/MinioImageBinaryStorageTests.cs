using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Infrastructure.Configuration.Images;
using AmusementPark.Infrastructure.Services.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Minio;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Images;

public sealed class MinioImageBinaryStorageTests
{
    [Fact]
    public async Task ExecuteWithVariantGenerationLeaseAsync_ShouldHoldLeaseUntilGenerationCompletes()
    {
        Mock<IImageVariantGenerationLease> lease =
            new Mock<IImageVariantGenerationLease>(MockBehavior.Strict);
        lease.Setup(value => value.TryAcquireAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        lease.Setup(value => value.ReleaseAsync(
                "comment/image-1",
                It.IsAny<string>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        MinioImageBinaryStorage storage = new MinioImageBinaryStorage(
            Mock.Of<IMinioClient>(),
            new MinioImageStorageSettings(),
            lease.Object,
            NullLogger<MinioImageBinaryStorage>.Instance);
        TaskCompletionSource<bool> generationStarted =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> allowGenerationCompletion =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        Task<(Stream Stream, string ContentType)?> generationTask =
            storage.ExecuteWithVariantGenerationLeaseAsync(
                "comment/image-1",
                async cancellationToken =>
                {
                    generationStarted.SetResult(true);
                    await allowGenerationCompletion.Task.WaitAsync(
                        cancellationToken);
                    return null;
                },
                CancellationToken.None);

        await generationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        lease.Verify(value => value.ReleaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        allowGenerationCompletion.SetResult(true);
        await generationTask;

        lease.Verify(value => value.ReleaseAsync(
                "comment/image-1",
                It.IsAny<string>(),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithVariantGenerationLeaseAsync_WhenCleanupOwnsClaim_ShouldNotGenerate()
    {
        Mock<IImageVariantGenerationLease> lease =
            new Mock<IImageVariantGenerationLease>(MockBehavior.Strict);
        lease.Setup(value => value.TryAcquireAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        MinioImageBinaryStorage storage = new MinioImageBinaryStorage(
            Mock.Of<IMinioClient>(),
            new MinioImageStorageSettings(),
            lease.Object,
            NullLogger<MinioImageBinaryStorage>.Instance);
        bool generationInvoked = false;

        (Stream Stream, string ContentType)? result =
            await storage.ExecuteWithVariantGenerationLeaseAsync(
                "comment/image-1",
                cancellationToken =>
                {
                    generationInvoked = true;
                    return Task.FromResult<(Stream Stream, string ContentType)?>(
                        null);
                },
                CancellationToken.None);

        Assert.Null(result);
        Assert.False(generationInvoked);
        lease.Verify(value => value.ReleaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteWithCommentDraftUploadLeaseAsync_ShouldReleaseOnlyAfterSuccessfulUpload()
    {
        Mock<IImageVariantGenerationLease> lease =
            new Mock<IImageVariantGenerationLease>(MockBehavior.Strict);
        lease.Setup(value => value.TryAcquireAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.Is<DateTime>(until => until > DateTime.UtcNow.AddHours(24)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        lease.Setup(value => value.ReleaseAsync(
                "comment/image-1",
                It.IsAny<string>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        MinioImageBinaryStorage storage = CreateStorage(lease);
        TaskCompletionSource<bool> uploadStarted =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> allowUploadCompletion =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        Task<IReadOnlyCollection<string>> uploadTask =
            storage.ExecuteWithCommentDraftUploadLeaseAsync(
                "comment/image-1",
                async cancellationToken =>
                {
                    uploadStarted.SetResult(true);
                    await allowUploadCompletion.Task.WaitAsync(
                        cancellationToken);
                    return new[] { "comment/image-1.webp" };
                },
                TimeSpan.FromHours(25),
                TimeSpan.FromHours(1),
                CancellationToken.None);

        await uploadStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        lease.Verify(value => value.ReleaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        allowUploadCompletion.SetResult(true);
        IReadOnlyCollection<string> result = await uploadTask;

        Assert.Single(result);
        lease.VerifyAll();
    }

    [Fact]
    public async Task ExecuteWithCommentDraftUploadLeaseAsync_WhenUploadFails_ShouldKeepLeaseForQuarantine()
    {
        Mock<IImageVariantGenerationLease> lease =
            new Mock<IImageVariantGenerationLease>(MockBehavior.Strict);
        lease.Setup(value => value.TryAcquireAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        MinioImageBinaryStorage storage = CreateStorage(lease);

        await Assert.ThrowsAsync<IOException>(() =>
            storage.ExecuteWithCommentDraftUploadLeaseAsync(
                "comment/image-1",
                _ => Task.FromException<IReadOnlyCollection<string>>(
                    new IOException("Ambiguous PUT.")),
                TimeSpan.FromHours(25),
                TimeSpan.FromHours(1),
                CancellationToken.None));

        lease.Verify(value => value.ReleaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        lease.VerifyAll();
    }

    [Fact]
    public async Task ExecuteWithCommentDraftUploadLeaseAsync_WhenUploadIsLong_ShouldRenewBeforeRelease()
    {
        Mock<IImageVariantGenerationLease> lease =
            new Mock<IImageVariantGenerationLease>(MockBehavior.Strict);
        lease.Setup(value => value.TryAcquireAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        TaskCompletionSource<bool> renewed =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        lease.Setup(value => value.RenewAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.Is<DateTime>(until => until > DateTime.UtcNow.AddHours(24)),
                It.IsAny<CancellationToken>()))
            .Callback(() => renewed.TrySetResult(true))
            .ReturnsAsync(true);
        lease.Setup(value => value.ReleaseAsync(
                "comment/image-1",
                It.IsAny<string>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        MinioImageBinaryStorage storage = CreateStorage(lease);

        IReadOnlyCollection<string> result =
            await storage.ExecuteWithCommentDraftUploadLeaseAsync(
                "comment/image-1",
                async cancellationToken =>
                {
                    await renewed.Task.WaitAsync(cancellationToken);
                    return new[] { "comment/image-1.webp" };
                },
                TimeSpan.FromHours(25),
                TimeSpan.FromMilliseconds(10),
                CancellationToken.None);

        Assert.Single(result);
        lease.Verify(value => value.RenewAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        lease.VerifyAll();
    }

    [Fact]
    public async Task ExecuteWithCommentDraftUploadLeaseAsync_WhenRenewalIsLost_ShouldCancelAndKeepLease()
    {
        Mock<IImageVariantGenerationLease> lease =
            new Mock<IImageVariantGenerationLease>(MockBehavior.Strict);
        lease.Setup(value => value.TryAcquireAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        lease.Setup(value => value.RenewAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        MinioImageBinaryStorage storage = CreateStorage(lease);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.ExecuteWithCommentDraftUploadLeaseAsync(
                "comment/image-1",
                async cancellationToken =>
                {
                    await Task.Delay(
                        Timeout.InfiniteTimeSpan,
                        cancellationToken);
                    return Array.Empty<string>();
                },
                TimeSpan.FromHours(25),
                TimeSpan.FromMilliseconds(10),
                CancellationToken.None));

        lease.Verify(value => value.ReleaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        lease.VerifyAll();
    }

    [Fact]
    public async Task ExecuteWithCommentDraftUploadLeaseAsync_WhenRenewalThrows_ShouldCancelAndKeepLease()
    {
        Mock<IImageVariantGenerationLease> lease =
            new Mock<IImageVariantGenerationLease>(MockBehavior.Strict);
        lease.Setup(value => value.TryAcquireAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        lease.Setup(value => value.RenewAsync(
                "comment/image-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Lease store unavailable."));
        MinioImageBinaryStorage storage = CreateStorage(lease);
        TaskCompletionSource<bool> uploadCancelled =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                storage.ExecuteWithCommentDraftUploadLeaseAsync(
                    "comment/image-1",
                    async cancellationToken =>
                    {
                        try
                        {
                            await Task.Delay(
                                Timeout.InfiniteTimeSpan,
                                cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            uploadCancelled.TrySetResult(true);
                            throw;
                        }

                        return Array.Empty<string>();
                    },
                    TimeSpan.FromHours(25),
                    TimeSpan.FromMilliseconds(10),
                    CancellationToken.None));

        Assert.Equal(
            "The comment draft upload lease was lost.",
            exception.Message);
        Assert.True(
            await uploadCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        lease.Verify(value => value.ReleaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        lease.VerifyAll();
    }

    [Fact]
    public async Task LoadForStorageAsync_WhenPrivateImageIsAnimated_ShouldDecodeOnlyFirstFrame()
    {
        await using MemoryStream stream = new MemoryStream();
        using (Image<Rgba32> image = new Image<Rgba32>(2, 3))
        using (Image<Rgba32> secondFrameImage = new Image<Rgba32>(2, 3))
        using (Image<Rgba32> thirdFrameImage = new Image<Rgba32>(2, 3))
        {
            image.Frames.AddFrame(secondFrameImage.Frames.RootFrame);
            image.Frames.AddFrame(thirdFrameImage.Frames.RootFrame);
            await image.SaveAsync(stream, new GifEncoder());
        }

        stream.Position = 0;
        using Image decoded = await MinioImageBinaryStorage.LoadForStorageAsync(
            stream,
            stripMetadata: true,
            CancellationToken.None);

        Assert.Single(decoded.Frames);
    }

    [Fact]
    public void StripEmbeddedMetadata_ShouldRemoveExifProfiles()
    {
        using Image<Rgba32> image = new Image<Rgba32>(1, 1);
        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitudeRef, "N");

        MinioImageBinaryStorage.StripEmbeddedMetadata(image);

        Assert.Null(image.Metadata.ExifProfile);
        Assert.Null(image.Metadata.IccProfile);
        Assert.Null(image.Metadata.IptcProfile);
        Assert.Null(image.Metadata.XmpProfile);
    }

    [Fact]
    public void AutoOrientAndStripEmbeddedMetadata_ShouldPreserveExifOrientationInPixels()
    {
        using Image<Rgba32> image = new Image<Rgba32>(2, 1);
        Rgba32 red = new Rgba32(255, 0, 0);
        Rgba32 blue = new Rgba32(0, 0, 255);
        image[0, 0] = red;
        image[1, 0] = blue;
        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)6);

        MinioImageBinaryStorage.AutoOrientAndStripEmbeddedMetadata(image);

        Assert.Equal(1, image.Width);
        Assert.Equal(2, image.Height);
        Assert.Equal(red, image[0, 0]);
        Assert.Equal(blue, image[0, 1]);
        Assert.Null(image.Metadata.ExifProfile);
    }

    private static MinioImageBinaryStorage CreateStorage(
        Mock<IImageVariantGenerationLease> lease)
    {
        return new MinioImageBinaryStorage(
            Mock.Of<IMinioClient>(),
            new MinioImageStorageSettings(),
            lease.Object,
            NullLogger<MinioImageBinaryStorage>.Instance);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(1, 320)]
    [InlineData(320, 320)]
    [InlineData(321, 480)]
    [InlineData(480, 480)]
    [InlineData(481, 640)]
    [InlineData(640, 640)]
    [InlineData(641, 800)]
    [InlineData(800, 800)]
    [InlineData(801, 960)]
    [InlineData(960, 960)]
    [InlineData(961, 1280)]
    [InlineData(1280, 1280)]
    [InlineData(1281, 1600)]
    [InlineData(1600, 1600)]
    [InlineData(1601, 1920)]
    [InlineData(1920, 1920)]
    [InlineData(3000, 1920)]
    public void NormalizeResponsiveWidth_ShouldClampToSupportedVariant(int? requestedWidth, int? expectedWidth)
    {
        int? result = MinioImageBinaryStorage.NormalizeResponsiveWidth(requestedWidth);

        Assert.Equal(expectedWidth, result);
    }

    [Fact]
    public void GetObjectNamesForDeletion_ShouldIncludeOriginalAndResponsiveVariants()
    {
        string[] objectNames = MinioImageBinaryStorage.GetObjectNamesForDeletion("images/photo-1").ToArray();

        Assert.Contains("images/photo-1.webp", objectNames);
        Assert.Contains("images/photo-1.jpg", objectNames);
        Assert.Contains("images/photo-1.jpeg", objectNames);
        Assert.Contains("images/photo-1.png", objectNames);
        Assert.Contains("images/photo-1.w320.v2.webp", objectNames);
        Assert.Contains("images/photo-1.w320.v2.jpg", objectNames);
        Assert.Contains("images/photo-1.w1600.v2.webp", objectNames);
        Assert.Contains("images/photo-1.w1600.v2.jpg", objectNames);
        Assert.Contains("images/photo-1.w320.webp", objectNames);
        Assert.Contains("images/photo-1.w320.jpg", objectNames);
        Assert.Contains("images/photo-1.w1920.webp", objectNames);
        Assert.Contains("images/photo-1.w1920.jpg", objectNames);
        Assert.Contains("images/photo-1.social.w960.v1.jpg", objectNames);
        Assert.DoesNotContain("images/photo-1.w321.webp", objectNames);
        Assert.Equal(objectNames.Length, objectNames.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void GetResponsiveVariantObjectName_ShouldIncludeCurrentVariantVersion()
    {
        string objectName = MinioImageBinaryStorage.GetResponsiveVariantObjectName("images/photo-1", 960, "webp");

        Assert.Equal("images/photo-1.w960.v2.webp", objectName);
    }

    [Fact]
    public void GetSocialPreviewVariantObjectName_ShouldIncludeDedicatedVariantVersion()
    {
        string objectName = MinioImageBinaryStorage.GetSocialPreviewVariantObjectName("images/photo-1", 960);

        Assert.Equal("images/photo-1.social.w960.v1.jpg", objectName);
    }
}
