using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class UploadImageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenGenericUploadTargetsCommentLifecycle_ShouldRejectBeforeProcessing()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageProcessingPipeline> imageProcessingPipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            imageRepository.Object,
            imageProcessingPipeline.Object,
            imageBinaryStorage.Object);

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Comment,
                File = CreateAvatarFile(100, "image/png"),
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                IsPublished = false,
            }));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "image.comment.lifecycle-managed");
        imageRepository.VerifyNoOtherCalls();
        imageProcessingPipeline.VerifyNoOtherCalls();
        imageBinaryStorage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenAvatarIsValid_ShouldStripMetadataAndPersistNoLocation()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageProcessingPipeline> imageProcessingPipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            imageRepository.Object,
            imageProcessingPipeline.Object,
            imageBinaryStorage.Object);
        FilePayload file = CreateAvatarFile(1024, "image/gif");
        imageProcessingPipeline
            .Setup(pipeline => pipeline.ExtractMetadataAsync(
                It.Is<ImageUploadRequest>(request => request.Category == ImageCategory.Avatar),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1200,
                Height = 800,
                SizeInBytes = file.Length,
                DetectedContentType = "image/jpeg",
                FrameCount = 1,
                GeoLocation = new GeoPointValue(50.0, 3.0),
                ExifMetadata = new ImageExifMetadata { CameraMaker = "Phone" },
            });
        imageBinaryStorage
            .Setup(storage => storage.SaveWithoutMetadataAsync(
                It.Is<string>(path => path.StartsWith("avatar/", StringComparison.Ordinal)),
                file,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "avatar/avatar.webp", "avatar/avatar.jpg" });
        imageRepository
            .Setup(repository => repository.CreateAsync(
                It.Is<ImageUploadRequest>(request =>
                    request.Category == ImageCategory.Avatar
                    && request.GeoLocation == null
                    && request.ExifMetadata == null
                    && request.File.ContentType == "image/jpeg"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImageUploadRequest request, CancellationToken _) => new Image
            {
                Id = request.ImageId ?? "image-id",
                Category = request.Category,
                OriginalFileName = request.File.FileName,
                ContentType = request.File.ContentType,
                Path = request.StoragePath,
            });

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Avatar,
                File = file,
                WithWatermark = false,
                OwnerType = ImageOwnerType.User,
                OwnerId = "user-1",
            }));

        Assert.True(result.IsSuccess);
        imageProcessingPipeline.VerifyAll();
        imageBinaryStorage.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Theory]
    [InlineData(4097, 100)]
    [InlineData(3000, 3000)]
    public async Task HandleAsync_WhenAvatarDimensionsAreUnsafe_ShouldRejectBeforeStorage(
        int width,
        int height)
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageProcessingPipeline> imageProcessingPipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            imageRepository.Object,
            imageProcessingPipeline.Object,
            imageBinaryStorage.Object);
        FilePayload file = CreateAvatarFile(1024);
        imageProcessingPipeline
            .Setup(pipeline => pipeline.ExtractMetadataAsync(
                It.IsAny<ImageUploadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = width,
                Height = height,
                SizeInBytes = file.Length,
                DetectedContentType = "image/jpeg",
                FrameCount = 1,
            });

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Avatar,
                File = file,
                WithWatermark = false,
                OwnerType = ImageOwnerType.User,
                OwnerId = "user-1",
            }));

        Assert.False(result.IsSuccess);
        imageProcessingPipeline.VerifyAll();
        imageBinaryStorage.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenAvatarFileIsUnsafe_ShouldRejectBeforeInspection()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageProcessingPipeline> imageProcessingPipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            imageRepository.Object,
            imageProcessingPipeline.Object,
            imageBinaryStorage.Object);
        FilePayload file = new FilePayload
        {
            FileName = "avatar",
            ContentType = "image/jpeg",
            Length = 5242881,
            Content = new MemoryStream(new byte[] { 1 }),
        };

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Avatar,
                File = file,
                WithWatermark = false,
            }));

        Assert.False(result.IsSuccess);
        imageProcessingPipeline.VerifyNoOtherCalls();
        imageBinaryStorage.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("image/gif", 1)]
    [InlineData("image/jpeg", 2)]
    public async Task HandleAsync_WhenDetectedAvatarContentIsUnsafe_ShouldRejectBeforeStorage(
        string detectedContentType,
        int frameCount)
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageProcessingPipeline> imageProcessingPipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            imageRepository.Object,
            imageProcessingPipeline.Object,
            imageBinaryStorage.Object);
        FilePayload file = CreateAvatarFile(1024);
        imageProcessingPipeline
            .Setup(pipeline => pipeline.ExtractMetadataAsync(
                It.IsAny<ImageUploadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1200,
                Height = 800,
                SizeInBytes = file.Length,
                DetectedContentType = detectedContentType,
                FrameCount = frameCount,
            });

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Avatar,
                File = file,
                WithWatermark = false,
                OwnerType = ImageOwnerType.User,
                OwnerId = "user-1",
            }));

        Assert.False(result.IsSuccess);
        imageProcessingPipeline.VerifyAll();
        imageBinaryStorage.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenLogoRequestsWatermark_ShouldSaveWithoutWatermark()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageProcessingPipeline> imageProcessingPipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            imageRepository.Object,
            imageProcessingPipeline.Object,
            imageBinaryStorage.Object);

        FilePayload file = new FilePayload
        {
            FileName = "logo.png",
            ContentType = "image/png",
            Length = 8,
            Content = new MemoryStream(new byte[] { 137, 80, 78, 71, 0, 0, 0, 0 }),
        };

        imageProcessingPipeline
            .Setup(pipeline => pipeline.ExtractMetadataAsync(
                It.Is<ImageUploadRequest>(request => request.Category == ImageCategory.Logo && request.WithWatermark),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1,
                Height = 1,
                SizeInBytes = file.Length,
            });

        imageBinaryStorage
            .Setup(storage => storage.SaveAsync(
                It.Is<string>(path => path.StartsWith("logo/", StringComparison.Ordinal)),
                file,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "logo/logo.webp", "logo/logo.jpg" });

        imageRepository
            .Setup(repository => repository.CreateAsync(
                It.Is<ImageUploadRequest>(request =>
                    request.Category == ImageCategory.Logo &&
                    !request.WithWatermark &&
                    request.StoragePath != null &&
                    request.StoragePath.StartsWith("logo/", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImageUploadRequest request, CancellationToken _) => new Image
            {
                Id = request.ImageId ?? "image-id",
                Category = request.Category,
                OriginalFileName = request.File!.FileName,
                ContentType = request.File.ContentType,
                Path = request.StoragePath,
            });

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(new UploadImageCommand(new ImageUploadRequest
        {
            Category = ImageCategory.Logo,
            File = file,
            WithWatermark = true,
        }));

        Assert.True(result.IsSuccess);
        Assert.Equal(ImageCategory.Logo, result.Value?.Image.Category);
        imageProcessingPipeline.VerifyAll();
        imageBinaryStorage.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCommentImage_ShouldStripMetadataAndRemainUnpublished()
    {
        DateTime startedAtUtc = DateTime.UtcNow;
        FilePayload file = new FilePayload
        {
            FileName = "comment.jpg",
            ContentType = "image/jpeg",
            Length = 128,
            Content = new MemoryStream(new byte[] { 1, 2, 3 }),
        };
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageProcessingPipeline> pipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        pipeline
            .Setup(value => value.ExtractMetadataAsync(It.IsAny<ImageUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1200,
                Height = 800,
                SizeInBytes = file.Length,
                DetectedContentType = "image/jpeg",
                FrameCount = 1,
                GeoLocation = new GeoPointValue(50, 3),
                ExifMetadata = new ImageExifMetadata { CameraMaker = "Private camera" },
            });
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        string? preparedImageId = null;
        string? preparedUploadToken = null;
        DateTime? preparedCleanupRequestedAtUtc = null;
        imageRepository.InSequence(sequence)
            .Setup(value => value.CreateAsync(
                It.Is<ImageUploadRequest>(request =>
                    request.Category == ImageCategory.Comment
                    && request.OwnerType == ImageOwnerType.CommentDraft
                    && request.OwnerId == "author-1"
                    && !request.IsPublished
                    && request.GeoLocation == null
                    && request.ExifMetadata == null
                    && !string.IsNullOrWhiteSpace(request.CommentDraftUploadToken)
                    && request.CleanupRequestedAtUtc.HasValue),
                It.IsAny<CancellationToken>()))
            .Callback((ImageUploadRequest request, CancellationToken _) =>
            {
                preparedImageId = request.ImageId;
                preparedUploadToken = request.CommentDraftUploadToken;
                preparedCleanupRequestedAtUtc = request.CleanupRequestedAtUtc;
            })
            .ReturnsAsync((ImageUploadRequest request, CancellationToken _) => new Image
            {
                Id = request.ImageId!,
                Category = request.Category,
                OwnerType = request.OwnerType,
                OwnerId = request.OwnerId,
                IsPublished = request.IsPublished,
                CleanupRequestedAtUtc = request.CleanupRequestedAtUtc,
            });
        storage.InSequence(sequence)
            .Setup(value => value.SaveWithoutMetadataAsync(
                It.Is<string>(path => path.StartsWith("comment/", StringComparison.Ordinal)),
                file,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "comment/image.webp", "comment/image.jpg" });
        imageRepository.InSequence(sequence)
            .Setup(value => value.CompleteCommentDraftUploadAsync(
                It.Is<string>(imageId => imageId == preparedImageId),
                "author-1",
                It.Is<string>(uploadToken => uploadToken == preparedUploadToken),
                It.Is<DateTime>(cleanupRequestedAtUtc =>
                    cleanupRequestedAtUtc == preparedCleanupRequestedAtUtc),
                CancellationToken.None))
            .ReturnsAsync(() => new Image
            {
                Id = preparedImageId!,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                IsPublished = false,
            });
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            imageRepository.Object,
            pipeline.Object,
            storage.Object);

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Comment,
                File = file,
                WithWatermark = true,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                IsPublished = false,
            }, AllowManagedCommentLifecycle: true));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Image.IsPublished);
        Assert.False(string.IsNullOrWhiteSpace(preparedImageId));
        Assert.False(string.IsNullOrWhiteSpace(preparedUploadToken));
        Assert.True(preparedCleanupRequestedAtUtc.HasValue);
        Assert.True(
            preparedCleanupRequestedAtUtc.Value
                >= startedAtUtc.AddHours(23));
        pipeline.VerifyAll();
        storage.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Theory]
    [InlineData(8193, 100)]
    [InlineData(7000, 7000)]
    public async Task HandleAsync_WhenCommentImageDimensionsAreUnsafe_ShouldRejectBeforeStorage(
        int width,
        int height)
    {
        FilePayload file = new FilePayload
        {
            FileName = "comment.jpg",
            ContentType = "image/jpeg",
            Length = 128,
            Content = new MemoryStream(new byte[] { 1, 2, 3 }),
        };
        Mock<IImageProcessingPipeline> pipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        pipeline.Setup(value => value.ExtractMetadataAsync(
                It.IsAny<ImageUploadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = width,
                Height = height,
                SizeInBytes = file.Length,
                DetectedContentType = "image/jpeg",
                FrameCount = 1,
            });
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            Mock.Of<IImageRepository>(),
            pipeline.Object,
            Mock.Of<IImageBinaryStorage>());

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Comment,
                File = file,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                IsPublished = false,
            }, AllowManagedCommentLifecycle: true));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "comment.image.dimensions-invalid");
        pipeline.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCommentImageDetectedFormatIsNotAllowed_ShouldRejectBeforeStorage()
    {
        FilePayload file = new FilePayload
        {
            FileName = "fake.png",
            ContentType = "image/png",
            Length = 128,
            Content = new MemoryStream(new byte[] { 1, 2, 3 }),
        };
        Mock<IImageProcessingPipeline> pipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        pipeline.Setup(value => value.ExtractMetadataAsync(
                It.IsAny<ImageUploadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 100,
                Height = 100,
                SizeInBytes = file.Length,
                DetectedContentType = "image/gif",
            });
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            Mock.Of<IImageRepository>(),
            pipeline.Object,
            Mock.Of<IImageBinaryStorage>());

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Comment,
                File = file,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                IsPublished = false,
            }, AllowManagedCommentLifecycle: true));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.invalid");
        pipeline.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCommentStorageFails_ShouldKeepDocumentMarkedForCleanup()
    {
        FilePayload file = new FilePayload
        {
            FileName = "comment.jpg",
            ContentType = "image/jpeg",
            Length = 128,
            Content = new MemoryStream(new byte[] { 1, 2, 3 }),
        };
        Mock<IImageProcessingPipeline> pipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        pipeline.Setup(value => value.ExtractMetadataAsync(
                It.IsAny<ImageUploadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1200,
                Height = 800,
                SizeInBytes = file.Length,
                DetectedContentType = "image/jpeg",
                FrameCount = 1,
            });
        Image draft = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            Path = "comment/image-1",
            IsPublished = false,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        repository.InSequence(sequence)
            .Setup(value => value.CreateAsync(
                It.Is<ImageUploadRequest>(request =>
                    request.Category == ImageCategory.Comment
                    && request.OwnerId == "author-1"
                    && !string.IsNullOrWhiteSpace(request.CommentDraftUploadToken)
                    && request.CleanupRequestedAtUtc.HasValue
                    && request.CleanupRequestedAtUtc.Value > DateTime.UtcNow),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        storage.InSequence(sequence)
            .Setup(value => value.SaveWithoutMetadataAsync(
                It.IsAny<string>(),
                file,
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Partial MinIO write."));
        repository.InSequence(sequence)
            .Setup(value => value.RequestCommentDraftCleanupAsync(
                "image-1",
                "author-1",
                It.IsAny<DateTime>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            repository.Object,
            pipeline.Object,
            storage.Object);

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Comment,
                File = file,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                WithWatermark = true,
                IsPublished = false,
            }, AllowManagedCommentLifecycle: true));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.invalid");
        pipeline.VerifyAll();
        repository.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCommentDraftCompletionReturnsNull_ShouldRequestImmediateCleanupAndFail()
    {
        DateTime startedAtUtc = DateTime.UtcNow;
        FilePayload file = new FilePayload
        {
            FileName = "comment.jpg",
            ContentType = "image/jpeg",
            Length = 128,
            Content = new MemoryStream(new byte[] { 1, 2, 3 }),
        };
        Mock<IImageProcessingPipeline> pipeline = CreateValidCommentPipeline(file);
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        string? uploadToken = null;
        DateTime? observedCleanupRequestedAtUtc = null;
        DateTime? requestedCleanupAtUtc = null;
        repository.InSequence(sequence)
            .Setup(value => value.CreateAsync(
                It.Is<ImageUploadRequest>(request =>
                    !string.IsNullOrWhiteSpace(request.CommentDraftUploadToken)
                    && request.CleanupRequestedAtUtc.HasValue),
                It.IsAny<CancellationToken>()))
            .Callback((ImageUploadRequest request, CancellationToken _) =>
            {
                uploadToken = request.CommentDraftUploadToken;
                observedCleanupRequestedAtUtc = request.CleanupRequestedAtUtc;
            })
            .ReturnsAsync(new Image
            {
                Id = "image-1",
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                IsPublished = false,
            });
        storage.InSequence(sequence)
            .Setup(value => value.SaveWithoutMetadataAsync(
                It.IsAny<string>(),
                file,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "comment/image.webp" });
        repository.InSequence(sequence)
            .Setup(value => value.CompleteCommentDraftUploadAsync(
                "image-1",
                "author-1",
                It.Is<string>(value => value == uploadToken),
                It.Is<DateTime>(value => value == observedCleanupRequestedAtUtc),
                CancellationToken.None))
            .ReturnsAsync((Image?)null);
        repository.InSequence(sequence)
            .Setup(value => value.RequestCommentDraftCleanupAsync(
                "image-1",
                "author-1",
                It.IsAny<DateTime>(),
                CancellationToken.None))
            .Callback((string _, string _, DateTime cleanupAtUtc, CancellationToken _) =>
                requestedCleanupAtUtc = cleanupAtUtc)
            .ReturnsAsync(true);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            repository.Object,
            pipeline.Object,
            storage.Object);

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            CreateManagedCommentUploadCommand(file));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.invalid");
        Assert.True(observedCleanupRequestedAtUtc > startedAtUtc.AddHours(23));
        Assert.True(requestedCleanupAtUtc.HasValue);
        Assert.InRange(requestedCleanupAtUtc.Value, startedAtUtc, DateTime.UtcNow);
        pipeline.VerifyAll();
        repository.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCommentDraftCompletionThrows_ShouldRequestImmediateCleanupAndFail()
    {
        DateTime startedAtUtc = DateTime.UtcNow;
        FilePayload file = new FilePayload
        {
            FileName = "comment.jpg",
            ContentType = "image/jpeg",
            Length = 128,
            Content = new MemoryStream(new byte[] { 1, 2, 3 }),
        };
        Mock<IImageProcessingPipeline> pipeline = CreateValidCommentPipeline(file);
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        string? uploadToken = null;
        DateTime? observedCleanupRequestedAtUtc = null;
        DateTime? requestedCleanupAtUtc = null;
        repository.InSequence(sequence)
            .Setup(value => value.CreateAsync(
                It.IsAny<ImageUploadRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback((ImageUploadRequest request, CancellationToken _) =>
            {
                uploadToken = request.CommentDraftUploadToken;
                observedCleanupRequestedAtUtc = request.CleanupRequestedAtUtc;
            })
            .ReturnsAsync(new Image
            {
                Id = "image-1",
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                IsPublished = false,
            });
        storage.InSequence(sequence)
            .Setup(value => value.SaveWithoutMetadataAsync(
                It.IsAny<string>(),
                file,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "comment/image.webp" });
        repository.InSequence(sequence)
            .Setup(value => value.CompleteCommentDraftUploadAsync(
                "image-1",
                "author-1",
                It.Is<string>(value => value == uploadToken),
                It.Is<DateTime>(value => value == observedCleanupRequestedAtUtc),
                CancellationToken.None))
            .ThrowsAsync(new IOException("Ambiguous completion."));
        repository.InSequence(sequence)
            .Setup(value => value.RequestCommentDraftCleanupAsync(
                "image-1",
                "author-1",
                It.IsAny<DateTime>(),
                CancellationToken.None))
            .Callback((string _, string _, DateTime cleanupAtUtc, CancellationToken _) =>
                requestedCleanupAtUtc = cleanupAtUtc)
            .ReturnsAsync(true);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            repository.Object,
            pipeline.Object,
            storage.Object);

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            CreateManagedCommentUploadCommand(file));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.invalid");
        Assert.False(string.IsNullOrWhiteSpace(uploadToken));
        Assert.True(observedCleanupRequestedAtUtc > startedAtUtc.AddHours(23));
        Assert.True(requestedCleanupAtUtc.HasValue);
        Assert.InRange(requestedCleanupAtUtc.Value, startedAtUtc, DateTime.UtcNow);
        pipeline.VerifyAll();
        repository.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCommentDraftCompletionIsCanceled_ShouldRequestImmediateCleanupAndRethrow()
    {
        DateTime startedAtUtc = DateTime.UtcNow;
        FilePayload file = new FilePayload
        {
            FileName = "comment.jpg",
            ContentType = "image/jpeg",
            Length = 128,
            Content = new MemoryStream(new byte[] { 1, 2, 3 }),
        };
        using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Mock<IImageProcessingPipeline> pipeline = CreateValidCommentPipeline(file);
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        string? uploadToken = null;
        DateTime? observedCleanupRequestedAtUtc = null;
        DateTime? requestedCleanupAtUtc = null;
        repository.InSequence(sequence)
            .Setup(value => value.CreateAsync(
                It.IsAny<ImageUploadRequest>(),
                cancellationTokenSource.Token))
            .Callback((ImageUploadRequest request, CancellationToken _) =>
            {
                uploadToken = request.CommentDraftUploadToken;
                observedCleanupRequestedAtUtc = request.CleanupRequestedAtUtc;
            })
            .ReturnsAsync(new Image
            {
                Id = "image-1",
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                IsPublished = false,
            });
        storage.InSequence(sequence)
            .Setup(value => value.SaveWithoutMetadataAsync(
                It.IsAny<string>(),
                file,
                true,
                cancellationTokenSource.Token))
            .ReturnsAsync(new[] { "comment/image.webp" });
        repository.InSequence(sequence)
            .Setup(value => value.CompleteCommentDraftUploadAsync(
                "image-1",
                "author-1",
                It.Is<string>(value => value == uploadToken),
                It.Is<DateTime>(value => value == observedCleanupRequestedAtUtc),
                CancellationToken.None))
            .Callback(() => cancellationTokenSource.Cancel())
            .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));
        repository.InSequence(sequence)
            .Setup(value => value.RequestCommentDraftCleanupAsync(
                "image-1",
                "author-1",
                It.IsAny<DateTime>(),
                CancellationToken.None))
            .Callback((string _, string _, DateTime cleanupAtUtc, CancellationToken _) =>
                requestedCleanupAtUtc = cleanupAtUtc)
            .ReturnsAsync(true);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            repository.Object,
            pipeline.Object,
            storage.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => handler.HandleAsync(
            CreateManagedCommentUploadCommand(file),
            cancellationTokenSource.Token));

        Assert.False(string.IsNullOrWhiteSpace(uploadToken));
        Assert.True(observedCleanupRequestedAtUtc > startedAtUtc.AddHours(23));
        Assert.True(requestedCleanupAtUtc.HasValue);
        Assert.InRange(requestedCleanupAtUtc.Value, startedAtUtc, DateTime.UtcNow);
        pipeline.VerifyAll();
        repository.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCommentDraftCreationFailsAfterAnAmbiguousInsert_ShouldRequestCleanup()
    {
        FilePayload file = new FilePayload
        {
            FileName = "comment.jpg",
            ContentType = "image/jpeg",
            Length = 128,
            Content = new MemoryStream(new byte[] { 1, 2, 3 }),
        };
        Mock<IImageProcessingPipeline> pipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        pipeline.Setup(value => value.ExtractMetadataAsync(
                It.IsAny<ImageUploadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1200,
                Height = 800,
                SizeInBytes = file.Length,
                DetectedContentType = "image/jpeg",
                FrameCount = 1,
            });
        DateTime startedAtUtc = DateTime.UtcNow;
        string? attemptedImageId = null;
        string? attemptedUploadToken = null;
        DateTime? attemptedCleanupRequestedAtUtc = null;
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.CreateAsync(
                It.Is<ImageUploadRequest>(request =>
                    request.Category == ImageCategory.Comment
                    && request.OwnerId == "author-1"
                    && !string.IsNullOrWhiteSpace(request.CommentDraftUploadToken)
                    && request.CleanupRequestedAtUtc.HasValue),
                It.IsAny<CancellationToken>()))
            .Callback((ImageUploadRequest request, CancellationToken _) =>
            {
                attemptedImageId = request.ImageId;
                attemptedUploadToken = request.CommentDraftUploadToken;
                attemptedCleanupRequestedAtUtc = request.CleanupRequestedAtUtc;
            })
            .ThrowsAsync(new IOException("Ambiguous Mongo insert."));
        repository
            .Setup(value => value.RequestCommentDraftCleanupAsync(
                It.Is<string>(imageId => imageId == attemptedImageId),
                "author-1",
                It.IsAny<DateTime>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            repository.Object,
            pipeline.Object,
            storage.Object);

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Comment,
                File = file,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                IsPublished = false,
            }, AllowManagedCommentLifecycle: true));

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(attemptedImageId));
        Assert.False(string.IsNullOrWhiteSpace(attemptedUploadToken));
        Assert.True(attemptedCleanupRequestedAtUtc > startedAtUtc.AddHours(23));
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.invalid");
        pipeline.VerifyAll();
        repository.VerifyAll();
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCommentDraftCreationIsCanceledAfterAnAmbiguousInsert_ShouldRequestCleanup()
    {
        FilePayload file = new FilePayload
        {
            FileName = "comment.jpg",
            ContentType = "image/jpeg",
            Length = 128,
            Content = new MemoryStream(new byte[] { 1, 2, 3 }),
        };
        Mock<IImageProcessingPipeline> pipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        pipeline.Setup(value => value.ExtractMetadataAsync(
                It.IsAny<ImageUploadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1200,
                Height = 800,
                SizeInBytes = file.Length,
                DetectedContentType = "image/jpeg",
                FrameCount = 1,
            });
        using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        DateTime startedAtUtc = DateTime.UtcNow;
        string? attemptedImageId = null;
        string? attemptedUploadToken = null;
        DateTime? attemptedCleanupRequestedAtUtc = null;
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.CreateAsync(
                It.IsAny<ImageUploadRequest>(),
                cancellationTokenSource.Token))
            .Callback((ImageUploadRequest request, CancellationToken _) =>
            {
                attemptedImageId = request.ImageId;
                attemptedUploadToken = request.CommentDraftUploadToken;
                attemptedCleanupRequestedAtUtc = request.CleanupRequestedAtUtc;
            })
            .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));
        repository
            .Setup(value => value.RequestCommentDraftCleanupAsync(
                It.Is<string>(imageId => imageId == attemptedImageId),
                "author-1",
                It.IsAny<DateTime>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            repository.Object,
            pipeline.Object,
            storage.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => handler.HandleAsync(
            new UploadImageCommand(new ImageUploadRequest
            {
                Category = ImageCategory.Comment,
                File = file,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                IsPublished = false,
            }, AllowManagedCommentLifecycle: true),
            cancellationTokenSource.Token));

        Assert.False(string.IsNullOrWhiteSpace(attemptedImageId));
        Assert.False(string.IsNullOrWhiteSpace(attemptedUploadToken));
        Assert.True(attemptedCleanupRequestedAtUtc > startedAtUtc.AddHours(23));
        pipeline.VerifyAll();
        repository.VerifyAll();
        storage.VerifyNoOtherCalls();
    }

    private static Mock<IImageProcessingPipeline> CreateValidCommentPipeline(FilePayload file)
    {
        Mock<IImageProcessingPipeline> pipeline =
            new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        pipeline.Setup(value => value.ExtractMetadataAsync(
                It.IsAny<ImageUploadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1200,
                Height = 800,
                SizeInBytes = file.Length,
                DetectedContentType = "image/jpeg",
                FrameCount = 1,
            });
        return pipeline;
    }

    private static UploadImageCommand CreateManagedCommentUploadCommand(
        FilePayload file)
    {
        return new UploadImageCommand(
            new ImageUploadRequest
            {
                Category = ImageCategory.Comment,
                File = file,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                WithWatermark = true,
                IsPublished = false,
            },
            AllowManagedCommentLifecycle: true);
    }

    private static FilePayload CreateAvatarFile(long length, string contentType = "image/jpeg")
    {
        return new FilePayload
        {
            FileName = "avatar.jpg",
            ContentType = contentType,
            Length = length,
            Content = new MemoryStream(new byte[length]),
        };
    }
}
