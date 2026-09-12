using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Application.Features.Users.Handlers;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Users;

public sealed class ProvisionExternalUserCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenExternalAvatarIsImported_ShouldAcquireLeaseBeforeImport()
    {
        VerifiedExternalIdentity identity = CreateIdentity(
            "external-user@example.com",
            "provider-user-avatar",
            "https://images.example.test/avatar.png");
        User existingUser = new User
        {
            Id = "existing-user-id",
            Email = identity.Email,
            UpdatedAtUtc = new DateTime(2026, 9, 6, 14, 0, 0, DateTimeKind.Utc),
            PublicDisplayName = "CoasterFan",
            UsesAutomaticPublicDisplayName = false,
            Roles = new List<Role> { Role.User },
            IsActivated = true,
            IsBlocked = false,
            ExternalLogins = new List<ExternalLogin>
            {
                new ExternalLogin
                {
                    Provider = ExternalLoginProvider.Google,
                    ProviderUserId = identity.ProviderUserId,
                },
            },
        };
        existingUser.AssignPublicAccountNumber(12);
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            "personal-ranking:existing-user-id");
        bool leaseStarted = false;
        ExternalUserHandlerMocks mocks = new ExternalUserHandlerMocks();
        SetupAuthenticationFlow(mocks, identity);
        mocks.UserRepository
            .Setup(repository => repository.GetByExternalLoginAsync(
                ExternalLoginProvider.Google,
                identity.ProviderUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        Mock<IPersonalRankingShareSourceRevisionGuard> revisions =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        revisions.Setup(value => value.BeginAvatarMutationAsync(
                "existing-user-id",
                It.IsAny<CancellationToken>()))
            .Callback(() => leaseStarted = true)
            .ReturnsAsync(lease);
        mocks.UserAvatarImporter
            .Setup(importer => importer.DownloadAndSaveAsync(
                identity.PictureUrl!,
                "existing-user-id",
                It.IsAny<CancellationToken>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => Assert.True(leaseStarted))
            .ReturnsAsync("/images/avatar-1");
        revisions.Setup(value => value.CompleteMutationAsync(
                lease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        SetupSuccessfulSignIn(
            mocks,
            user => user.AvatarUrl == "/images/avatar-1");
        ProvisionExternalUserCommandHandler handler = CreateHandler(
            mocks,
            revisions.Object);

        ApplicationResult<AuthenticatedUserResult> result = await handler.HandleAsync(
            CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal("/images/avatar-1", result.Value!.User.AvatarUrl);
        mocks.UserRepository.Verify(repository => repository.UpdateIfUnchangedAsync(
            "existing-user-id",
            It.IsAny<User>(),
            new DateTime(2026, 9, 6, 14, 0, 0, DateTimeKind.Utc),
            It.IsAny<CancellationToken>()));
        mocks.VerifyAll();
        revisions.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenCreatingExternalUser_ShouldAllocateAndKeepTheAutomaticPublicIdentity()
    {
        VerifiedExternalIdentity identity = CreateIdentity("external-user@example.com", "provider-user-7");
        ExternalUserHandlerMocks mocks = new ExternalUserHandlerMocks();
        User? createdUser = null;
        SetupAuthenticationFlow(mocks, identity);
        mocks.UserRepository
            .Setup(repository => repository.GetByExternalLoginAsync(
                ExternalLoginProvider.Google,
                identity.ProviderUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        mocks.UserRepository
            .Setup(repository => repository.GetByEmailAsync(
                identity.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        mocks.UserRepository
            .Setup(repository => repository.AllocatePublicAccountNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(7L);
        mocks.UserRepository
            .Setup(repository => repository.CreateAsync(
                It.Is<User>(user =>
                    user.PublicAccountNumber == 7
                    && user.PublicDisplayName == "User0007"
                    && user.UsesAutomaticPublicDisplayName),
                It.IsAny<CancellationToken>()))
            .Callback((User user, CancellationToken _) => createdUser = user)
            .ReturnsAsync((User user, CancellationToken _) => user);
        SetupSuccessfulSignIn(mocks, static user => user.PublicAccountNumber == 7);
        ProvisionExternalUserCommandHandler handler = CreateHandler(mocks);

        ApplicationResult<AuthenticatedUserResult> result = await handler.HandleAsync(
            CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.Same(createdUser, result.Value!.User);
        Assert.Equal(7, result.Value.User.PublicAccountNumber);
        Assert.Equal("User0007", result.Value.User.PublicDisplayName);
        Assert.True(result.Value.User.UsesAutomaticPublicDisplayName);
        mocks.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenAvatarImportWinsButUserCompareAndSwapLoses_ShouldReconcileAvatar()
    {
        VerifiedExternalIdentity identity = CreateIdentity(
            "external-user@example.com",
            "provider-user-conflict",
            "https://images.example.test/avatar.png");
        DateTime expectedUpdatedAtUtc = new DateTime(
            2026,
            9,
            6,
            16,
            0,
            0,
            DateTimeKind.Utc);
        User staleUser = new User
        {
            Id = "existing-user-id",
            Email = identity.Email,
            UpdatedAtUtc = expectedUpdatedAtUtc,
            PublicDisplayName = "CoasterFan",
            UsesAutomaticPublicDisplayName = false,
            Roles = new List<Role> { Role.User },
            IsActivated = true,
            IsBlocked = false,
            ExternalLogins = new List<ExternalLogin>
            {
                new ExternalLogin
                {
                    Provider = ExternalLoginProvider.Google,
                    ProviderUserId = identity.ProviderUserId,
                },
            },
        };
        staleUser.AssignPublicAccountNumber(12);
        User currentUser = new User
        {
            Id = staleUser.Id,
            Email = staleUser.Email,
            UpdatedAtUtc = expectedUpdatedAtUtc.AddSeconds(1),
            PublicDisplayName = staleUser.PublicDisplayName,
            UsesAutomaticPublicDisplayName = false,
            Roles = new List<Role> { Role.User, Role.Admin },
            IsActivated = true,
            IsBlocked = false,
        };
        currentUser.AssignPublicAccountNumber(12);
        Image currentAvatar = new Image
        {
            Id = "avatar-1",
            OwnerType = ImageOwnerType.User,
            OwnerId = staleUser.Id,
            Category = ImageCategory.Avatar,
            IsCurrent = true,
            IsPublished = true,
        };
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            "personal-ranking:existing-user-id");
        ExternalUserHandlerMocks mocks = new ExternalUserHandlerMocks();
        SetupAuthenticationFlow(mocks, identity);
        mocks.UserRepository
            .Setup(repository => repository.GetByExternalLoginAsync(
                ExternalLoginProvider.Google,
                identity.ProviderUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleUser);
        mocks.UserRepository
            .Setup(repository => repository.UpdateIfUnchangedAsync(
                staleUser.Id,
                It.Is<User>(user => user.AvatarUrl == "/images/avatar-1"),
                expectedUpdatedAtUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        mocks.UserRepository
            .Setup(repository => repository.GetByIdAsync(
                staleUser.Id,
                CancellationToken.None))
            .ReturnsAsync(currentUser);
        mocks.UserRepository
            .Setup(repository => repository.UpdateAvatarUrlAsync(
                staleUser.Id,
                "/images/avatar-1",
                CancellationToken.None))
            .ReturnsAsync(true);
        mocks.ImageRepository
            .Setup(repository => repository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                staleUser.Id,
                ImageCategory.Avatar,
                CancellationToken.None))
            .ReturnsAsync(currentAvatar);
        mocks.UserAvatarImporter
            .Setup(importer => importer.DownloadAndSaveAsync(
                identity.PictureUrl!,
                staleUser.Id,
                It.IsAny<CancellationToken>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("/images/avatar-1");
        Mock<IPersonalRankingShareSourceRevisionGuard> revisions =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        revisions.Setup(value => value.BeginAvatarMutationAsync(
                staleUser.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        revisions.Setup(value => value.CompleteMutationAsync(
                lease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        ProvisionExternalUserCommandHandler handler = CreateHandler(
            mocks,
            revisions.Object);

        ApplicationResult<AuthenticatedUserResult> result = await handler.HandleAsync(
            CreateCommand());

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "user.update.failed");
        Assert.Contains(Role.Admin, currentUser.Roles);
        mocks.VerifyAll();
        revisions.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_WhenImportedAvatarUserWriteIsAmbiguous_ShouldReconcileInBothExistingUserPaths(
        bool foundByProvider)
    {
        VerifiedExternalIdentity identity = CreateIdentity(
            "external-user@example.com",
            "provider-user-timeout",
            "https://images.example.test/avatar.png");
        DateTime expectedUpdatedAtUtc = new DateTime(
            2026,
            9,
            6,
            17,
            0,
            0,
            DateTimeKind.Utc);
        User staleUser = new User
        {
            Id = "existing-user-id",
            Email = identity.Email,
            UpdatedAtUtc = expectedUpdatedAtUtc,
            PublicDisplayName = "CoasterFan",
            UsesAutomaticPublicDisplayName = false,
            Roles = new List<Role> { Role.User },
            IsActivated = true,
            IsBlocked = false,
            ExternalLogins = foundByProvider
                ? new List<ExternalLogin>
                {
                    new ExternalLogin
                    {
                        Provider = ExternalLoginProvider.Google,
                        ProviderUserId = identity.ProviderUserId,
                    },
                }
                : new List<ExternalLogin>(),
        };
        staleUser.AssignPublicAccountNumber(12);
        User authoritativeUser = new User
        {
            Id = staleUser.Id,
            Email = staleUser.Email,
            UpdatedAtUtc = expectedUpdatedAtUtc.AddSeconds(1),
            PublicDisplayName = staleUser.PublicDisplayName,
            UsesAutomaticPublicDisplayName = false,
            Roles = new List<Role> { Role.User },
            IsActivated = true,
            IsBlocked = false,
        };
        authoritativeUser.AssignPublicAccountNumber(12);
        Image currentAvatar = new Image
        {
            Id = "avatar-1",
            OwnerType = ImageOwnerType.User,
            OwnerId = staleUser.Id,
            Category = ImageCategory.Avatar,
            IsCurrent = true,
            IsPublished = true,
        };
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            "personal-ranking:existing-user-id");
        ExternalUserHandlerMocks mocks = new ExternalUserHandlerMocks();
        SetupAuthenticationFlow(mocks, identity);
        mocks.UserRepository
            .Setup(repository => repository.GetByExternalLoginAsync(
                ExternalLoginProvider.Google,
                identity.ProviderUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(foundByProvider ? staleUser : null);
        if (!foundByProvider)
        {
            mocks.UserRepository
                .Setup(repository => repository.GetByEmailAsync(
                    identity.Email,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(staleUser);
        }

        mocks.UserRepository
            .Setup(repository => repository.UpdateIfUnchangedAsync(
                staleUser.Id,
                It.Is<User>(user => user.AvatarUrl == "/images/avatar-1"),
                expectedUpdatedAtUtc,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("MongoDB response was ambiguous."));
        mocks.UserRepository
            .Setup(repository => repository.GetByIdAsync(
                staleUser.Id,
                CancellationToken.None))
            .ReturnsAsync(authoritativeUser);
        mocks.UserRepository
            .Setup(repository => repository.UpdateAvatarUrlAsync(
                staleUser.Id,
                "/images/avatar-1",
                CancellationToken.None))
            .Callback((string _, string? avatarUrl, CancellationToken _) =>
                authoritativeUser.AvatarUrl = avatarUrl)
            .ReturnsAsync(true);
        mocks.ImageRepository
            .Setup(repository => repository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                staleUser.Id,
                ImageCategory.Avatar,
                CancellationToken.None))
            .ReturnsAsync(currentAvatar);
        mocks.UserAvatarImporter
            .Setup(importer => importer.DownloadAndSaveAsync(
                identity.PictureUrl!,
                staleUser.Id,
                It.IsAny<CancellationToken>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("/images/avatar-1");
        Mock<IPersonalRankingShareSourceRevisionGuard> revisions =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        revisions.Setup(value => value.BeginAvatarMutationAsync(
                staleUser.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        revisions.Setup(value => value.CompleteMutationAsync(
                lease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        ProvisionExternalUserCommandHandler handler = CreateHandler(
            mocks,
            revisions.Object);

        await Assert.ThrowsAsync<TimeoutException>(() => handler.HandleAsync(
            CreateCommand()));

        Assert.Equal("/images/avatar-1", authoritativeUser.AvatarUrl);
        mocks.VerifyAll();
        revisions.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLegacyExternalUserHasCustomName_ShouldAllocateNumberWithoutReplacingName()
    {
        VerifiedExternalIdentity identity = CreateIdentity("legacy-user@example.com", "provider-user-legacy");
        User legacyUser = new User
        {
            Id = "legacy-user-id",
            Email = identity.Email,
            PublicDisplayName = "CoasterFan",
            UsesAutomaticPublicDisplayName = false,
            Roles = new List<Role> { Role.User },
            IsActivated = true,
            IsBlocked = false,
            ExternalLogins = new List<ExternalLogin>
            {
                new ExternalLogin
                {
                    Provider = ExternalLoginProvider.Google,
                    ProviderUserId = identity.ProviderUserId,
                },
            },
        };
        ExternalUserHandlerMocks mocks = new ExternalUserHandlerMocks();
        SetupAuthenticationFlow(mocks, identity);
        mocks.UserRepository
            .Setup(repository => repository.GetByExternalLoginAsync(
                ExternalLoginProvider.Google,
                identity.ProviderUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(legacyUser);
        mocks.UserRepository
            .Setup(repository => repository.AllocatePublicAccountNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(12L);
        SetupSuccessfulSignIn(
            mocks,
            user =>
                ReferenceEquals(user, legacyUser)
                && user.PublicAccountNumber == 12
                && user.PublicDisplayName == "CoasterFan"
                && !user.UsesAutomaticPublicDisplayName);
        ProvisionExternalUserCommandHandler handler = CreateHandler(mocks);

        ApplicationResult<AuthenticatedUserResult> result = await handler.HandleAsync(
            CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value!.User.PublicAccountNumber);
        Assert.Equal("CoasterFan", result.Value.User.PublicDisplayName);
        Assert.False(result.Value.User.UsesAutomaticPublicDisplayName);
        mocks.VerifyAll();
    }

    private static ProvisionExternalUserCommandHandler CreateHandler(
        ExternalUserHandlerMocks mocks,
        IPersonalRankingShareSourceRevisionGuard? shareSourceRevisionGuard = null)
    {
        return new ProvisionExternalUserCommandHandler(
            mocks.UserRepository.Object,
            mocks.ImageRepository.Object,
            mocks.ExternalIdentityVerifier.Object,
            mocks.UserAvatarImporter.Object,
            mocks.TokenService.Object,
            mocks.RefreshTokenFactory.Object,
            mocks.RefreshTokenRepository.Object,
            mocks.AuthenticationSettings.Object,
            shareSourceRevisionGuard ?? Mock.Of<IPersonalRankingShareSourceRevisionGuard>());
    }

    private static void SetupAuthenticationFlow(
        ExternalUserHandlerMocks mocks,
        VerifiedExternalIdentity identity)
    {
        mocks.ExternalIdentityVerifier
            .Setup(verifier => verifier.Supports(ExternalLoginProvider.Google))
            .Returns(true);
        mocks.ExternalIdentityVerifier
            .Setup(verifier => verifier.VerifyAsync(
                ExternalLoginProvider.Google,
                "provider-token",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(identity);
    }

    private static void SetupSuccessfulSignIn(
        ExternalUserHandlerMocks mocks,
        Func<User, bool> updatedUserPredicate)
    {
        mocks.UserRepository
            .Setup(repository => repository.UpdateIfUnchangedAsync(
                It.IsAny<string>(),
                It.Is<User>(user => updatedUserPredicate(user)),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, User user, DateTime _, CancellationToken _) => user);
        mocks.TokenService
            .Setup(service => service.GenerateUserToken(It.Is<User>(user => updatedUserPredicate(user))))
            .Returns("access-token");
        mocks.RefreshTokenFactory
            .Setup(factory => factory.Generate())
            .Returns("refresh-token");
        mocks.RefreshTokenFactory
            .Setup(factory => factory.ComputeHash("refresh-token"))
            .Returns("refresh-token-hash");
        mocks.AuthenticationSettings
            .SetupGet(settings => settings.TokenRefreshLimitMinutes)
            .Returns(60);
        mocks.RefreshTokenRepository
            .Setup(repository => repository.CreateAsync(
                It.Is<RefreshToken>(token =>
                    token.TokenHash == "refresh-token-hash"
                    && token.UserId.Length > 0),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static ProvisionExternalUserCommand CreateCommand()
    {
        return new ProvisionExternalUserCommand(new ProvisionExternalUserRequest
        {
            Provider = ExternalLoginProvider.Google,
            Token = "provider-token",
            PreferredLanguage = "fr",
            PreferredMeasurementSystem = "Metric",
        });
    }

    private static VerifiedExternalIdentity CreateIdentity(
        string email,
        string providerUserId,
        string? pictureUrl = null)
    {
        return new VerifiedExternalIdentity
        {
            Provider = ExternalLoginProvider.Google,
            ProviderUserId = providerUserId,
            Email = email,
            IsEmailVerified = true,
            IsEmailAuthoritative = true,
            DisplayName = "External user",
            GivenName = "External",
            FamilyName = "User",
            PictureUrl = pictureUrl,
        };
    }

    private sealed class ExternalUserHandlerMocks
    {
        public Mock<IUserRepository> UserRepository { get; } =
            new Mock<IUserRepository>(MockBehavior.Strict);

        public Mock<IImageRepository> ImageRepository { get; } =
            new Mock<IImageRepository>(MockBehavior.Strict);

        public Mock<IExternalIdentityVerifier> ExternalIdentityVerifier { get; } =
            new Mock<IExternalIdentityVerifier>(MockBehavior.Strict);

        public Mock<IUserAvatarImporter> UserAvatarImporter { get; } =
            new Mock<IUserAvatarImporter>(MockBehavior.Strict);

        public Mock<ITokenService> TokenService { get; } =
            new Mock<ITokenService>(MockBehavior.Strict);

        public Mock<IRefreshTokenFactory> RefreshTokenFactory { get; } =
            new Mock<IRefreshTokenFactory>(MockBehavior.Strict);

        public Mock<IRefreshTokenRepository> RefreshTokenRepository { get; } =
            new Mock<IRefreshTokenRepository>(MockBehavior.Strict);

        public Mock<IUserAuthenticationSettings> AuthenticationSettings { get; } =
            new Mock<IUserAuthenticationSettings>(MockBehavior.Strict);

        public void VerifyAll()
        {
            UserRepository.VerifyAll();
            ImageRepository.VerifyAll();
            ExternalIdentityVerifier.VerifyAll();
            UserAvatarImporter.VerifyAll();
            TokenService.VerifyAll();
            RefreshTokenFactory.VerifyAll();
            RefreshTokenRepository.VerifyAll();
            AuthenticationSettings.VerifyAll();
        }
    }
}
