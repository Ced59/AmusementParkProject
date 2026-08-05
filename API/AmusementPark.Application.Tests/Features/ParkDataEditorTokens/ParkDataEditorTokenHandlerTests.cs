using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkDataEditorTokens.Commands;
using AmusementPark.Application.Features.ParkDataEditorTokens.Handlers;
using AmusementPark.Application.Features.ParkDataEditorTokens.Ports;
using AmusementPark.Application.Features.ParkDataEditorTokens.Queries;
using AmusementPark.Application.Features.ParkDataEditorTokens.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkDataEditorTokens;

public sealed class ParkDataEditorTokenHandlerTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistOnlyProtectedMaterialForEligibleUser()
    {
        User user = CreateEligibleUser();
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository.Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        Mock<IParkDataEditorAccessTokenRepository> tokenRepository =
            new Mock<IParkDataEditorAccessTokenRepository>(MockBehavior.Strict);
        tokenRepository.Setup(repository => repository.CountActiveByUserIdAsync(
                user.Id,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        ParkDataEditorAccessToken? persistedToken = null;
        tokenRepository.Setup(repository => repository.CreateAsync(
                It.IsAny<ParkDataEditorAccessToken>(),
                It.IsAny<CancellationToken>()))
            .Callback<ParkDataEditorAccessToken, CancellationToken>((token, _) => persistedToken = token)
            .Returns(Task.CompletedTask);
        Mock<IParkDataEditorTokenProtector> protector = new Mock<IParkDataEditorTokenProtector>(MockBehavior.Strict);
        protector.Setup(item => item.Create(It.IsAny<string>()))
            .Returns(new ParkDataEditorTokenMaterial("plain-secret", "stored-hash", "apf_pde_12345678"));
        CreateParkDataEditorTokenCommandHandler handler = new CreateParkDataEditorTokenCommandHandler(
            userRepository.Object,
            tokenRepository.Object,
            protector.Object);

        ApplicationResult<CreatedParkDataEditorTokenResult> result = await handler.HandleAsync(
            new CreateParkDataEditorTokenCommand(user.Id, "Codex production", 30));

        Assert.True(result.IsSuccess);
        Assert.Equal("plain-secret", result.Value?.PlainTextToken);
        Assert.NotNull(persistedToken);
        Assert.Equal("stored-hash", persistedToken.TokenHash);
        Assert.DoesNotContain("plain-secret", persistedToken.TokenHash, StringComparison.Ordinal);
        userRepository.VerifyAll();
        tokenRepository.VerifyAll();
        protector.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectUserWithoutDedicatedRole()
    {
        User user = CreateEligibleUser();
        user.Roles = new List<Role> { Role.User };
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository.Setup(repository => repository.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        CreateParkDataEditorTokenCommandHandler handler = new CreateParkDataEditorTokenCommandHandler(
            userRepository.Object,
            Mock.Of<IParkDataEditorAccessTokenRepository>(MockBehavior.Strict),
            Mock.Of<IParkDataEditorTokenProtector>(MockBehavior.Strict));

        ApplicationResult<CreatedParkDataEditorTokenResult> result = await handler.HandleAsync(
            new CreateParkDataEditorTokenCommand(user.Id, "Codex production", 30));

        Assert.False(result.IsSuccess);
        Assert.Equal("park-data-editor-token.user-not-eligible", Assert.Single(result.Errors).Code);
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReloadUserRoleAndTouchActiveToken()
    {
        DateTime expiresAtUtc = DateTime.UtcNow.AddDays(1);
        ParkDataEditorAccessToken token = new ParkDataEditorAccessToken
        {
            Id = "token-id",
            UserId = "user-id",
            TokenHash = "hash",
            ExpiresAtUtc = expiresAtUtc,
        };
        User user = CreateEligibleUser();
        string parsedTokenId = token.Id;
        Mock<IParkDataEditorTokenProtector> protector = new Mock<IParkDataEditorTokenProtector>(MockBehavior.Strict);
        protector.Setup(item => item.TryReadTokenId("plain-token", out parsedTokenId)).Returns(true);
        protector.Setup(item => item.Verify("plain-token", token)).Returns(true);
        Mock<IParkDataEditorAccessTokenRepository> tokenRepository =
            new Mock<IParkDataEditorAccessTokenRepository>(MockBehavior.Strict);
        tokenRepository.Setup(repository => repository.GetByIdAsync(token.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        tokenRepository.Setup(repository => repository.MarkUsedAsync(
                token.Id,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository.Setup(repository => repository.GetByIdAsync(token.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        AuthenticateParkDataEditorTokenQueryHandler handler = new AuthenticateParkDataEditorTokenQueryHandler(
            userRepository.Object,
            tokenRepository.Object,
            protector.Object);

        ApplicationResult<ParkDataEditorTokenAuthenticationResult> result = await handler.HandleAsync(
            new AuthenticateParkDataEditorTokenQuery("plain-token"));

        Assert.True(result.IsSuccess);
        Assert.Same(user, result.Value?.User);
        userRepository.VerifyAll();
        tokenRepository.VerifyAll();
        protector.VerifyAll();
    }

    [Fact]
    public async Task RevokeAsync_ShouldReturnNotFoundWhenNoActiveTokenMatches()
    {
        Mock<IParkDataEditorAccessTokenRepository> tokenRepository =
            new Mock<IParkDataEditorAccessTokenRepository>(MockBehavior.Strict);
        tokenRepository.Setup(repository => repository.RevokeAsync(
                "user-id",
                "token-id",
                "admin-id",
                "reason",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        RevokeParkDataEditorTokensCommandHandler handler = new RevokeParkDataEditorTokensCommandHandler(
            tokenRepository.Object);

        ApplicationResult<RevokedParkDataEditorTokensResult> result = await handler.HandleAsync(
            new RevokeParkDataEditorTokensCommand("user-id", "token-id", "admin-id", "reason"));

        Assert.False(result.IsSuccess);
        Assert.Equal("park-data-editor-token.not-found", Assert.Single(result.Errors).Code);
        tokenRepository.VerifyAll();
    }

    private static User CreateEligibleUser()
    {
        return new User
        {
            Id = "user-id",
            Email = "codex@example.com",
            IsActivated = true,
            Roles = new List<Role> { Role.User, Role.ParkDataEditor },
        };
    }
}
