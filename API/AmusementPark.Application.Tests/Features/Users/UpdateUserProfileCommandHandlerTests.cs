using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Handlers;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class UpdateUserProfileCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPublicIdentityChanges_ShouldAdvancePersonalShareSource()
    {
        User user = CreateUser("user-1", "OldName");
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository.Setup(value => value.GetByIdAsync(
                "user-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository.Setup(value => value.GetByPublicDisplayNameAsync(
                "NewName",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        userRepository.Setup(value => value.UpdateIfUnchangedAsync(
                "user-1",
                user,
                user.UpdatedAtUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, User updated, DateTime _, CancellationToken _) => updated);
        ShareSourceMutationLease mutationLease = new ShareSourceMutationLease(
            "personal-ranking:user-1",
            4.ToString("x32"));
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                "personal-ranking:user-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationLease);
        shareRevisions.Setup(value => value.CompleteMutationAsync(
                mutationLease,
                true,
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevision(2, 0, DateTime.UtcNow));
        UpdateUserProfileCommandHandler handler = CreateHandler(
            userRepository,
            shareRevisions.Object);

        ApplicationResult<User> result = await handler.HandleAsync(
            new UpdateUserProfileCommand("user-1", CreateUpdate("NewName")));

        Assert.True(result.IsSuccess);
        Assert.Equal("NewName", result.Value!.PublicDisplayName);
        userRepository.VerifyAll();
        shareRevisions.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenProfilePersistenceFails_ShouldAdvancePersonalShareSourceRevisionConservatively()
    {
        User user = CreateUser("user-1", "OldName");
        DateTime expectedUpdatedAtUtc = user.UpdatedAtUtc;
        Mock<IUserRepository> userRepository =
            new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository.Setup(value => value.GetByIdAsync(
                "user-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository.Setup(value => value.GetByPublicDisplayNameAsync(
                "NewName",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        userRepository.Setup(value => value.UpdateIfUnchangedAsync(
                "user-1",
                user,
                expectedUpdatedAtUtc,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("MongoDB timeout."));
        ShareSourceMutationLease mutationLease = ShareSourceMutationLease.Create(
            "personal-ranking:user-1");
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                mutationLease.ScopeKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationLease);
        shareRevisions.Setup(value => value.CompleteMutationAsync(
                mutationLease,
                true,
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevision(1, 0, DateTime.UtcNow));
        UpdateUserProfileCommandHandler handler = CreateHandler(
            userRepository,
            shareRevisions.Object);

        ApplicationResult<User> result = await handler.HandleAsync(
            new UpdateUserProfileCommand("user-1", CreateUpdate("NewName")));

        Assert.False(result.IsSuccess);
        userRepository.VerifyAll();
        shareRevisions.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPublicDisplayNameIsAvailable_ShouldPersistIt()
    {
        User user = CreateUser("user-1", null);
        user.AvatarUrl = "/images/avatar-existing";
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository
            .Setup(repository => repository.GetByPublicDisplayNameAsync(
                "CoasterFan",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        userRepository
            .Setup(repository => repository.UpdateIfUnchangedAsync(
                "user-1",
                user,
                user.UpdatedAtUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, User updated, DateTime _, CancellationToken _) => updated);
        UpdateUserProfileCommandHandler handler = CreateHandler(userRepository);

        ApplicationResult<User> result = await handler.HandleAsync(new UpdateUserProfileCommand(
            "user-1",
            CreateUpdate(" CoasterFan ")));

        Assert.True(result.IsSuccess);
        Assert.Equal("CoasterFan", result.Value!.PublicDisplayName);
        Assert.Equal("/images/avatar-existing", result.Value.AvatarUrl);
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPublicDisplayNameBelongsToAnotherUser_ShouldRejectIt()
    {
        User user = CreateUser("user-1", "User0001", true);
        User otherUser = CreateUser("user-2", "CoasterFan");
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository
            .Setup(repository => repository.GetByPublicDisplayNameAsync(
                "CoasterFan",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherUser);
        UpdateUserProfileCommandHandler handler = CreateHandler(userRepository);

        ApplicationResult<User> result = await handler.HandleAsync(new UpdateUserProfileCommand(
            "user-1",
            CreateUpdate("CoasterFan")));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "user.public-display-name.already-exists");
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPublicDisplayNameImpersonatesAStaffRole_ShouldRejectIt()
    {
        User user = CreateUser("user-1", "User0001", true);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        UpdateUserProfileCommandHandler handler = CreateHandler(userRepository);

        ApplicationResult<User> result = await handler.HandleAsync(new UpdateUserProfileCommand(
            "user-1",
            CreateUpdate("Adm1n-Support")));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "user.public-display-name.reserved");
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPublicDisplayNameIsCleared_ShouldRestoreTheAutomaticIdentifier()
    {
        User user = CreateUser("user-1", "CoasterFan");
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository
            .Setup(repository => repository.UpdateIfUnchangedAsync(
                "user-1",
                user,
                user.UpdatedAtUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, User updated, DateTime _, CancellationToken _) => updated);
        UpdateUserProfileCommandHandler handler = CreateHandler(userRepository);

        ApplicationResult<User> result = await handler.HandleAsync(new UpdateUserProfileCommand(
            "user-1",
            CreateUpdate(" ")));

        Assert.True(result.IsSuccess);
        Assert.Equal("User0001", result.Value!.PublicDisplayName);
        Assert.True(result.Value.UsesAutomaticPublicDisplayName);
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentAutomaticStaffIdentifierIsSubmitted_ShouldKeepIt()
    {
        User user = CreateUser("user-1", "Admin01", true);
        user.Roles = new List<Role> { Role.User, Role.Admin };
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository
            .Setup(repository => repository.GetByPublicDisplayNameAsync(
                "Admin01",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepository
            .Setup(repository => repository.UpdateIfUnchangedAsync(
                "user-1",
                user,
                user.UpdatedAtUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, User updated, DateTime _, CancellationToken _) => updated);
        UpdateUserProfileCommandHandler handler = CreateHandler(userRepository);

        ApplicationResult<User> result = await handler.HandleAsync(new UpdateUserProfileCommand(
            "user-1",
            CreateUpdate("Admin01")));

        Assert.True(result.IsSuccess);
        Assert.Equal("Admin01", result.Value!.PublicDisplayName);
        Assert.True(result.Value.UsesAutomaticPublicDisplayName);
        userRepository.VerifyAll();
    }

    private static UpdateUserProfileCommandHandler CreateHandler(
        Mock<IUserRepository> userRepository,
        IShareSourceRevisionRepository? shareSourceRevisions = null)
    {
        return new UpdateUserProfileCommandHandler(
            userRepository.Object,
            new Mock<IRefreshTokenFactory>(MockBehavior.Strict).Object,
            new Mock<ILocalAccountEmailService>(MockBehavior.Strict).Object,
            new Mock<IUserAuthenticationSettings>(MockBehavior.Strict).Object,
            new PersonalRankingShareSourceRevisionGuard(
                shareSourceRevisions ?? CreateShareSourceRevisionRepository(),
                NullLogger<PersonalRankingShareSourceRevisionGuard>.Instance));
    }

    private static IShareSourceRevisionRepository CreateShareSourceRevisionRepository()
    {
        Mock<IShareSourceRevisionRepository> repository =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Loose);
        repository.Setup(value => value.BeginMutationAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns((string scopeKey, CancellationToken _) => Task.FromResult(
                new ShareSourceMutationLease(scopeKey, 5.ToString("x32"))));
        repository.Setup(value => value.CompleteMutationAsync(
                It.IsAny<ShareSourceMutationLease>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShareSourceRevision(1, 0, DateTime.UtcNow));
        return repository.Object;
    }

    private static User CreateUser(
        string id,
        string? publicDisplayName,
        bool usesAutomaticPublicDisplayName = false)
    {
        User user = new User
        {
            Id = id,
            Email = "user@example.com",
            PublicDisplayName = publicDisplayName,
            UsesAutomaticPublicDisplayName = usesAutomaticPublicDisplayName,
            Roles = new List<Role> { Role.User },
        };
        user.AssignPublicAccountNumber(1);
        return user;
    }

    private static UserProfileUpdate CreateUpdate(string publicDisplayName)
    {
        return new UserProfileUpdate
        {
            Email = "user@example.com",
            NewEmail = "user@example.com",
            PublicDisplayName = publicDisplayName,
            PreferredLanguage = "fr",
            PreferredMeasurementSystem = "Metric",
        };
    }
}
