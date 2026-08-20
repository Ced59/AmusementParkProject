using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Handlers;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class UpdatePreferredLanguageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenLanguageIsSupported_ShouldPersistNormalizedPreference()
    {
        User updatedUser = new User
        {
            Id = "user-1",
            PreferredLanguage = "FR",
        };
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.UpdatePreferredLanguageAsync(
                "user-1",
                "FR",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedUser);
        UpdatePreferredLanguageCommandHandler handler = new UpdatePreferredLanguageCommandHandler(
            userRepository.Object);

        ApplicationResult<User> result = await handler.HandleAsync(
            new UpdatePreferredLanguageCommand(" user-1 ", " fr "));

        Assert.True(result.IsSuccess);
        Assert.Equal("FR", result.Value!.PreferredLanguage);
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLanguageIsUnsupported_ShouldRejectWithoutPersistence()
    {
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        UpdatePreferredLanguageCommandHandler handler = new UpdatePreferredLanguageCommandHandler(
            userRepository.Object);

        ApplicationResult<User> result = await handler.HandleAsync(
            new UpdatePreferredLanguageCommand("user-1", "ja"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "user.preferred-language.invalid");
        userRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.UpdatePreferredLanguageAsync(
                "user-1",
                "DE",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        UpdatePreferredLanguageCommandHandler handler = new UpdatePreferredLanguageCommandHandler(
            userRepository.Object);

        ApplicationResult<User> result = await handler.HandleAsync(
            new UpdatePreferredLanguageCommand("user-1", "de"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "user.not-found");
        userRepository.VerifyAll();
    }
}
