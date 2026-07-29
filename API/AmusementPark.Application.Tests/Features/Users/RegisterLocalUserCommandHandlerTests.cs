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

public sealed class RegisterLocalUserCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRegistrationSucceeds_ShouldAllocateAndKeepTheAutomaticPublicIdentity()
    {
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        Mock<IRefreshTokenFactory> refreshTokenFactory = new Mock<IRefreshTokenFactory>(MockBehavior.Strict);
        Mock<IPasswordHasher> passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        Mock<ILocalAccountEmailService> localAccountEmailService =
            new Mock<ILocalAccountEmailService>(MockBehavior.Strict);
        Mock<IUserAuthenticationSettings> authenticationSettings =
            new Mock<IUserAuthenticationSettings>(MockBehavior.Strict);
        User? persistedUser = null;

        userRepository
            .Setup(repository => repository.GetByEmailAsync(
                "new-user@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        userRepository
            .Setup(repository => repository.AllocatePublicAccountNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(7L);
        userRepository
            .Setup(repository => repository.CreateAsync(
                It.Is<User>(user =>
                    user.PublicAccountNumber == 7
                    && user.PublicDisplayName == "User0007"
                    && user.UsesAutomaticPublicDisplayName),
                It.IsAny<CancellationToken>()))
            .Callback((User user, CancellationToken _) => persistedUser = user)
            .ReturnsAsync((User user, CancellationToken _) => user);
        refreshTokenFactory
            .Setup(factory => factory.Generate())
            .Returns("confirmation-token");
        refreshTokenFactory
            .Setup(factory => factory.ComputeHash("confirmation-token"))
            .Returns("confirmation-token-hash");
        passwordHasher
            .Setup(hasher => hasher.HashPassword("Valid123!"))
            .Returns("hashed-password");
        localAccountEmailService
            .Setup(service => service.SendEmailConfirmationAsync(
                It.Is<User>(user => user.PublicAccountNumber == 7),
                "confirmation-token",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        authenticationSettings
            .SetupGet(settings => settings.EmailConfirmationTokenExpirationHours)
            .Returns(24);
        RegisterLocalUserCommandHandler handler = new RegisterLocalUserCommandHandler(
            userRepository.Object,
            refreshTokenFactory.Object,
            passwordHasher.Object,
            localAccountEmailService.Object,
            authenticationSettings.Object);

        ApplicationResult<User> result = await handler.HandleAsync(new RegisterLocalUserCommand(
            new RegisterUserRequest
            {
                Email = " NEW-USER@example.com ",
                Password = "Valid123!",
                VerifyPassword = "Valid123!",
                PreferredLanguage = "fr",
                PreferredMeasurementSystem = "Metric",
            }));

        Assert.True(result.IsSuccess);
        Assert.Same(persistedUser, result.Value);
        Assert.Equal(7, result.Value!.PublicAccountNumber);
        Assert.Equal("User0007", result.Value.PublicDisplayName);
        Assert.True(result.Value.UsesAutomaticPublicDisplayName);
        userRepository.VerifyAll();
        refreshTokenFactory.VerifyAll();
        passwordHasher.VerifyAll();
        localAccountEmailService.VerifyAll();
        authenticationSettings.VerifyAll();
    }
}
