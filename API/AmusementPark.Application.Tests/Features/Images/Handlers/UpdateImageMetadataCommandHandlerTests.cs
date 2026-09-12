using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class UpdateImageMetadataCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCurrentAvatarChangesOwner_ShouldFenceAndSynchronizeBothOwners()
    {
        DateTime updatedAtUtc = new DateTime(
            2026,
            9,
            6,
            18,
            0,
            0,
            DateTimeKind.Utc);
        Image existing = new Image
        {
            Id = "avatar-1",
            Category = ImageCategory.Avatar,
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-old",
            IsCurrent = true,
            IsPublished = true,
            UpdatedAtUtc = updatedAtUtc,
        };
        Image updated = new Image
        {
            Id = "avatar-1",
            Category = ImageCategory.Avatar,
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-new",
            IsCurrent = true,
            IsPublished = true,
        };
        User previousOwner = new User
        {
            Id = "owner-old",
            Email = "old@example.com",
            AvatarUrl = "/images/avatar-1",
        };
        User nextOwner = new User
        {
            Id = "owner-new",
            Email = "new@example.com",
        };
        ShareSourceMutationLease previousLease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-old");
        ShareSourceMutationLease nextLease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-new");
        bool previousLeaseStarted = false;
        bool nextLeaseStarted = false;

        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByIdAsync("avatar-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        images.Setup(value => value.UpdateMetadataIfUnchangedAsync(
                "avatar-1",
                It.Is<ImageMutationPrecondition>(guard =>
                    guard.OwnerType == ImageOwnerType.User
                    && guard.OwnerId == "owner-old"
                    && guard.Category == ImageCategory.Avatar
                    && guard.IsCurrent
                    && guard.UpdatedAtUtc == updatedAtUtc),
                It.Is<ImageMetadataUpdate>(metadata =>
                    metadata.OwnerId == "owner-new" && metadata.IsCurrent == true),
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                Assert.True(previousLeaseStarted);
                Assert.True(nextLeaseStarted);
            })
            .ReturnsAsync(updated);
        images.Setup(value => value.SetCurrentIfUnchangedAsync(
                "avatar-1",
                It.Is<ImageMutationPrecondition>(guard =>
                    guard.OwnerType == ImageOwnerType.User
                    && guard.OwnerId == "owner-new"
                    && guard.Category == ImageCategory.Avatar
                    && guard.IsCurrent),
                ImageOwnerType.User,
                "owner-new",
                It.IsAny<CancellationToken>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);
        images.Setup(value => value.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-old",
                ImageCategory.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);
        images.Setup(value => value.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-new",
                ImageCategory.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(value => value.GetByIdAsync("owner-old", It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousOwner);
        users.Setup(value => value.GetByIdAsync("owner-new", It.IsAny<CancellationToken>()))
            .ReturnsAsync(nextOwner);
        users.Setup(value => value.UpdateAvatarUrlAsync(
                "owner-old",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        users.Setup(value => value.UpdateAvatarUrlAsync(
                "owner-new",
                "/images/avatar-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IPersonalRankingShareSourceRevisionGuard> revisions =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        revisions.Setup(value => value.BeginAvatarMutationAsync(
                "owner-old",
                It.IsAny<CancellationToken>()))
            .Callback(() => previousLeaseStarted = true)
            .ReturnsAsync(previousLease);
        revisions.Setup(value => value.BeginAvatarMutationAsync(
                "owner-new",
                It.IsAny<CancellationToken>()))
            .Callback(() => nextLeaseStarted = true)
            .ReturnsAsync(nextLease);
        revisions.Setup(value => value.CompleteMutationAsync(
                previousLease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        revisions.Setup(value => value.CompleteMutationAsync(
                nextLease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        UpdateImageMetadataCommandHandler handler = new UpdateImageMetadataCommandHandler(
            images.Object,
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            users.Object,
            revisions.Object);

        ApplicationResult<Image> result = await handler.HandleAsync(
            new UpdateImageMetadataCommand("avatar-1", new ImageMetadataUpdate
            {
                Category = ImageCategory.Avatar,
                OwnerType = ImageOwnerType.User,
                OwnerId = "owner-new",
                IsCurrent = true,
                IsPublished = true,
            }));

        Assert.True(result.IsSuccess);
        images.VerifyAll();
        users.VerifyAll();
        revisions.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenImageBelongsToCommentLifecycle_ShouldRejectBeforeMutation()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByIdAsync(
                "comment-image",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = "comment-image",
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.Comment,
                OwnerId = "comment-1",
                IsPublished = true,
            });
        UpdateImageMetadataCommandHandler handler = new UpdateImageMetadataCommandHandler(
            imageRepository.Object,
            Mock.Of<IParkRepository>(),
            Mock.Of<IAttractionManufacturerRepository>(),
            Mock.Of<ISearchProjectionWriter>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IPersonalRankingShareSourceRevisionGuard>());

        ApplicationResult<Image> result = await handler.HandleAsync(
            new UpdateImageMetadataCommand("comment-image", new ImageMetadataUpdate
            {
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.Comment,
                OwnerId = "comment-1",
                IsPublished = false,
            }));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "image.comment.lifecycle-managed");
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenExpectedScopeChanged_ShouldRejectBeforeMutation()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByIdAsync(
                "image-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = "image-1",
                Category = ImageCategory.Avatar,
                OwnerType = ImageOwnerType.User,
                OwnerId = "owner-1",
                IsCurrent = true,
                IsPublished = true,
            });
        UpdateImageMetadataCommandHandler handler = new UpdateImageMetadataCommandHandler(
            imageRepository.Object,
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            Mock.Of<IUserRepository>(MockBehavior.Strict),
            Mock.Of<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict));

        ApplicationResult<Image> result = await handler.HandleAsync(
            new UpdateImageMetadataCommand(
                "image-1",
                new ImageMetadataUpdate
                {
                    Category = ImageCategory.Park,
                    OwnerType = ImageOwnerType.Park,
                    OwnerId = "park-1",
                    IsPublished = false,
                },
                SuppressSeoNotification: true,
                ExpectedState: new ImageMutationPrecondition(
                    ImageOwnerType.Park,
                    "park-1",
                    ImageCategory.Park,
                    false)));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "image.not-found");
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenExpectedTimestampChanged_ShouldRejectBeforeMutation()
    {
        DateTime currentUpdatedAtUtc = new DateTime(
            2026,
            9,
            6,
            18,
            30,
            0,
            DateTimeKind.Utc);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByIdAsync(
                "image-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = "image-1",
                Category = ImageCategory.Park,
                OwnerType = ImageOwnerType.Park,
                OwnerId = "park-1",
                IsCurrent = false,
                IsPublished = true,
                UpdatedAtUtc = currentUpdatedAtUtc,
            });
        UpdateImageMetadataCommandHandler handler = new UpdateImageMetadataCommandHandler(
            imageRepository.Object,
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            Mock.Of<IUserRepository>(MockBehavior.Strict),
            Mock.Of<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict));

        ApplicationResult<Image> result = await handler.HandleAsync(
            new UpdateImageMetadataCommand(
                "image-1",
                new ImageMetadataUpdate
                {
                    Category = ImageCategory.Park,
                    OwnerType = ImageOwnerType.Park,
                    OwnerId = "park-1",
                    IsPublished = false,
                },
                SuppressSeoNotification: true,
                ExpectedState: new ImageMutationPrecondition(
                    ImageOwnerType.Park,
                    "park-1",
                    ImageCategory.Park,
                    false,
                    currentUpdatedAtUtc.AddSeconds(-1))));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "image.not-found");
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentParkLogoLeavesLogoCategory_ShouldClearParkCurrentLogo()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);

        Image existing = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Logo,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            IsCurrent = true,
            IsPublished = true,
        };

        Image updated = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            IsCurrent = false,
            IsPublished = true,
        };

        Park park = new Park
        {
            Id = "park-1",
            Name = "Test Park",
            CurrentLogoImageId = "image-1",
        };

        imageRepository
            .Setup(repository => repository.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        imageRepository
            .Setup(repository => repository.UpdateMetadataIfUnchangedAsync(
                "image-1",
                It.IsAny<ImageMutationPrecondition>(),
                It.Is<ImageMetadataUpdate>(metadata =>
                    metadata.Category == ImageCategory.Park &&
                    metadata.OwnerType == ImageOwnerType.Park &&
                    metadata.OwnerId == "park-1" &&
                    metadata.IsCurrent == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        imageRepository
            .Setup(repository => repository.GetCurrentByOwnerAsync(ImageOwnerType.Park, "park-1", ImageCategory.Logo, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        parkRepository
            .Setup(repository => repository.UpdateAsync(
                "park-1",
                It.Is<Park>(value => value.CurrentLogoImageId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string parkId, Park value, CancellationToken cancellationToken) => value);

        UpdateImageMetadataCommandHandler handler = new UpdateImageMetadataCommandHandler(
            imageRepository.Object,
            parkRepository.Object,
            manufacturerRepository.Object,
            searchProjectionWriter.Object,
            userRepository.Object,
            Mock.Of<IPersonalRankingShareSourceRevisionGuard>());

        ApplicationResult<Image> result = await handler.HandleAsync(new UpdateImageMetadataCommand(" image-1 ", new ImageMetadataUpdate
        {
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Description = existing.Description,
            GeoLocation = null,
            AltTexts = Array.Empty<LocalizedTextValue>(),
            Captions = Array.Empty<LocalizedTextValue>(),
            Credits = Array.Empty<LocalizedTextValue>(),
            TagIds = Array.Empty<string>(),
            IsPublished = true,
            SourceUrl = null,
        }));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value?.IsCurrent);
        imageRepository.VerifyAll();
        parkRepository.VerifyAll();
        manufacturerRepository.VerifyNoOtherCalls();
        searchProjectionWriter.VerifyNoOtherCalls();
        userRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentAvatarBecomesPrivate_ShouldClearPublicAvatarUrl()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        Mock<IPersonalRankingShareSourceRevisionGuard> revisionGuard =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        Image existing = new Image
        {
            Id = "avatar-1",
            Category = ImageCategory.Avatar,
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            IsCurrent = true,
            IsPublished = true,
        };
        Image updated = new Image
        {
            Id = "avatar-1",
            Category = ImageCategory.Avatar,
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            IsCurrent = true,
            IsPublished = false,
        };
        User user = new User
        {
            Id = "owner-1",
            Email = "owner@example.com",
            AvatarUrl = "/images/avatar-1",
        };
        ShareSourceMutationLease mutationLease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-1");
        imageRepository
            .Setup(repository => repository.GetByIdAsync("avatar-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        imageRepository
            .Setup(repository => repository.UpdateMetadataIfUnchangedAsync(
                "avatar-1",
                It.Is<ImageMutationPrecondition>(guard =>
                    guard.OwnerType == ImageOwnerType.User
                    && guard.OwnerId == "owner-1"
                    && guard.Category == ImageCategory.Avatar
                    && guard.IsCurrent),
                It.Is<ImageMetadataUpdate>(metadata => metadata.IsPublished == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);
        imageRepository
            .Setup(repository => repository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);
        userRepository
            .Setup(repository => repository.GetByIdAsync("owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        revisionGuard
            .Setup(guard => guard.BeginAvatarMutationAsync(
                "owner-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationLease);
        userRepository
            .Setup(repository => repository.UpdateAvatarUrlAsync(
                "owner-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        revisionGuard
            .Setup(guard => guard.CompleteMutationAsync(
                mutationLease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        UpdateImageMetadataCommandHandler handler = new UpdateImageMetadataCommandHandler(
            imageRepository.Object,
            Mock.Of<IParkRepository>(),
            Mock.Of<IAttractionManufacturerRepository>(),
            Mock.Of<ISearchProjectionWriter>(),
            userRepository.Object,
            revisionGuard.Object);

        ApplicationResult<Image> result = await handler.HandleAsync(
            new UpdateImageMetadataCommand("avatar-1", new ImageMetadataUpdate
            {
                Category = ImageCategory.Avatar,
                OwnerType = ImageOwnerType.User,
                OwnerId = "owner-1",
                IsPublished = false,
            }));

        Assert.True(result.IsSuccess);
        imageRepository.VerifyAll();
        userRepository.VerifyAll();
        revisionGuard.VerifyAll();
    }
}
