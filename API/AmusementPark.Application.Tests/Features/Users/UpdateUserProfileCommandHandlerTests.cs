using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Handlers;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class UpdateUserProfileCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPublicDisplayNameIsAvailable_ShouldPersistIt()
    {
        User user = CreateUser("user-1", null);
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
            .Setup(repository => repository.UpdateAsync("user-1", user, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, User updated, CancellationToken _) => updated);
        UpdateUserProfileCommandHandler handler = CreateHandler(userRepository);

        ApplicationResult<User> result = await handler.HandleAsync(new UpdateUserProfileCommand(
            "user-1",
            CreateUpdate(" CoasterFan ")));

        Assert.True(result.IsSuccess);
        Assert.Equal("CoasterFan", result.Value!.PublicDisplayName);
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
            .Setup(repository => repository.UpdateAsync("user-1", user, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, User updated, CancellationToken _) => updated);
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
            .Setup(repository => repository.UpdateAsync("user-1", user, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, User updated, CancellationToken _) => updated);
        UpdateUserProfileCommandHandler handler = CreateHandler(userRepository);

        ApplicationResult<User> result = await handler.HandleAsync(new UpdateUserProfileCommand(
            "user-1",
            CreateUpdate("Admin01")));

        Assert.True(result.IsSuccess);
        Assert.Equal("Admin01", result.Value!.PublicDisplayName);
        Assert.True(result.Value.UsesAutomaticPublicDisplayName);
        userRepository.VerifyAll();
    }

    private static UpdateUserProfileCommandHandler CreateHandler(Mock<IUserRepository> userRepository)
    {
        return new UpdateUserProfileCommandHandler(
            userRepository.Object,
            new Mock<IRefreshTokenFactory>(MockBehavior.Strict).Object,
            new Mock<ILocalAccountEmailService>(MockBehavior.Strict).Object,
            new Mock<IUserAuthenticationSettings>(MockBehavior.Strict).Object);
    }

    private static User CreateUser(
        string id,
        string? publicDisplayName,
        bool usesAutomaticPublicDisplayName = false)
    {
        return new User
        {
            Id = id,
            Email = "user@example.com",
            PublicDisplayName = publicDisplayName,
            PublicAccountNumber = 1,
            UsesAutomaticPublicDisplayName = usesAutomaticPublicDisplayName,
            Roles = new List<Role> { Role.User },
        };
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
