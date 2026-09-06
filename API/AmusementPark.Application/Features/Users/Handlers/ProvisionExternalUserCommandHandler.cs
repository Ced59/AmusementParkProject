using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Results;
using AmusementPark.Application.Features.Images;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler d'authentification externe avec provisionnement automatique.
/// </summary>
public sealed class ProvisionExternalUserCommandHandler : ICommandHandler<ProvisionExternalUserCommand, ApplicationResult<AuthenticatedUserResult>>
{
    private readonly IUserRepository userRepository;
    private readonly IImageRepository imageRepository;
    private readonly IExternalIdentityVerifier externalIdentityVerifier;
    private readonly IUserAvatarImporter userAvatarImporter;
    private readonly ITokenService tokenService;
    private readonly IRefreshTokenFactory refreshTokenFactory;
    private readonly IRefreshTokenRepository refreshTokenRepository;
    private readonly IUserAuthenticationSettings authenticationSettings;
    private readonly IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard;

    public ProvisionExternalUserCommandHandler(
        IUserRepository userRepository,
        IImageRepository imageRepository,
        IExternalIdentityVerifier externalIdentityVerifier,
        IUserAvatarImporter userAvatarImporter,
        ITokenService tokenService,
        IRefreshTokenFactory refreshTokenFactory,
        IRefreshTokenRepository refreshTokenRepository,
        IUserAuthenticationSettings authenticationSettings,
        IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard)
    {
        this.userRepository = userRepository;
        this.imageRepository = imageRepository;
        this.externalIdentityVerifier = externalIdentityVerifier;
        this.userAvatarImporter = userAvatarImporter;
        this.tokenService = tokenService;
        this.refreshTokenFactory = refreshTokenFactory;
        this.refreshTokenRepository = refreshTokenRepository;
        this.authenticationSettings = authenticationSettings;
        this.shareSourceRevisionGuard = shareSourceRevisionGuard;
    }

    public async Task<ApplicationResult<AuthenticatedUserResult>> HandleAsync(ProvisionExternalUserCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(ApplicationErrors.Required(nameof(command.Request)));
        }

        if (!this.externalIdentityVerifier.Supports(command.Request.Provider))
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.ExternalAuthenticationProviderNotSupported());
        }

        VerifiedExternalIdentity? verifiedIdentity = await this.externalIdentityVerifier.VerifyAsync(
            command.Request.Provider,
            command.Request.Token,
            command.Request.Nonce,
            cancellationToken);

        if (verifiedIdentity is null)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.InvalidExternalIdentityToken());
        }

        User? userByProvider = await this.userRepository.GetByExternalLoginAsync(verifiedIdentity.Provider, verifiedIdentity.ProviderUserId, cancellationToken);
        if (userByProvider is not null)
        {
            return await this.SignInAsync(userByProvider, verifiedIdentity, cancellationToken);
        }

        User? userByEmail = await this.userRepository.GetByEmailAsync(UserRules.NormalizeEmail(verifiedIdentity.Email)!, cancellationToken);
        if (userByEmail is not null)
        {
            if (!CanAutoLink(userByEmail, verifiedIdentity))
            {
                return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.ExternalLoginRequiresAccountLinking());
            }

            ApplyIdentityToUser(userByEmail, verifiedIdentity, true);
            User? updatedExistingUser = await this.PersistUserAsync(userByEmail, verifiedIdentity, false, cancellationToken);
            if (updatedExistingUser is null)
            {
                return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.UserUpdateFailed());
            }

            return await this.SignInAsync(updatedExistingUser, verifiedIdentity, cancellationToken);
        }

        User newUser = BuildUserFromIdentity(verifiedIdentity, command.Request.PreferredLanguage, command.Request.PreferredMeasurementSystem);
        User? createdUser = await this.PersistUserAsync(newUser, verifiedIdentity, true, cancellationToken);
        if (createdUser is null)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.LoginFailed());
        }

        return await this.SignInAsync(createdUser, verifiedIdentity, cancellationToken);
    }

    private static User BuildUserFromIdentity(VerifiedExternalIdentity identity, string? preferredLanguage, string? preferredMeasurementSystem)
    {
        DateTime now = DateTime.UtcNow;

        User user = new User
        {
            Email = UserRules.NormalizeEmail(identity.Email),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PreferredLanguage = UserRules.NormalizePreferredLanguage(preferredLanguage),
            PreferredMeasurementSystem = UserRules.NormalizePreferredMeasurementSystem(preferredMeasurementSystem),
            IsActivated = true,
            IsBlocked = false,
            FirstName = identity.GivenName,
            LastName = identity.FamilyName,
            Roles = new List<Role> { Role.User },
            HashedPassword = string.Empty,
            LastLoginUtc = now,
            LastActivityUtc = now,
        };

        ApplyIdentityToUser(user, identity, true);
        return user;
    }

    private static void ApplyIdentityToUser(User user, VerifiedExternalIdentity identity, bool allowNameBackfill)
    {
        user.ExternalLogins ??= new List<ExternalLogin>();

        ExternalLogin? existingLogin = user.ExternalLogins.FirstOrDefault(login => login.Provider == identity.Provider);
        if (existingLogin is null)
        {
            existingLogin = new ExternalLogin
            {
                Provider = identity.Provider,
                LinkedAtUtc = DateTime.UtcNow,
            };
            user.ExternalLogins.Add(existingLogin);
        }

        existingLogin.ProviderUserId = identity.ProviderUserId;
        existingLogin.Email = UserRules.NormalizeEmail(identity.Email) ?? string.Empty;
        existingLogin.IsEmailVerified = identity.IsEmailVerified;
        existingLogin.DisplayName = identity.DisplayName;
        existingLogin.GivenName = identity.GivenName;
        existingLogin.FamilyName = identity.FamilyName;
        existingLogin.PictureUrl = identity.PictureUrl;
        existingLogin.HostedDomain = identity.HostedDomain;
        existingLogin.LastLoginAtUtc = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            user.Email = UserRules.NormalizeEmail(identity.Email);
        }

        if (allowNameBackfill)
        {
            if (string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(identity.GivenName))
            {
                user.FirstName = identity.GivenName;
            }

            if (string.IsNullOrWhiteSpace(user.LastName) && !string.IsNullOrWhiteSpace(identity.FamilyName))
            {
                user.LastName = identity.FamilyName;
            }
        }

    }

    private async Task<User?> PersistUserAsync(User user, VerifiedExternalIdentity identity, bool createIfMissing, CancellationToken cancellationToken)
    {
        DateTime expectedUpdatedAtUtc = user.UpdatedAtUtc;
        PersonalRankingShareIdentityState previousIdentity =
            PersonalRankingShareIdentityState.Capture(user);
        ShareSourceMutationLease? mutationLease = !createIfMissing
            && CanChangePublicIdentity(user, identity)
                ? await this.shareSourceRevisionGuard.BeginMutationAsync(
                    user.Id,
                    cancellationToken)
                : null;
        using CancellationTokenSource mutationCancellation =
            ShareSourceMutationCancellation.CreateLinkedSource(
                cancellationToken,
                mutationLease);
        bool sourceMutationAttempted = false;
        bool avatarImported = false;
        try
        {
            await this.EnsurePublicIdentityAsync(user, mutationCancellation.Token);

            if (ShouldImportAvatar(user, identity))
            {
                sourceMutationAttempted = mutationLease is not null;
                string avatarPath = await this.userAvatarImporter.DownloadAndSaveAsync(
                    identity.PictureUrl!,
                    user.Id,
                    mutationCancellation.Token);
                if (!string.IsNullOrWhiteSpace(avatarPath))
                {
                    user.AvatarUrl = avatarPath;
                    avatarImported = true;
                }
            }

            user.UpdatedAtUtc = DateTime.UtcNow;

            if (createIfMissing)
            {
                return await this.userRepository.CreateAsync(
                    user,
                    mutationCancellation.Token);
            }

            sourceMutationAttempted |= previousIdentity !=
                PersonalRankingShareIdentityState.Capture(user);
            User? updatedUser = await this.userRepository.UpdateIfUnchangedAsync(
                user.Id,
                user,
                expectedUpdatedAtUtc,
                mutationCancellation.Token);
            if (updatedUser is null && avatarImported)
            {
                await UserAvatarShareSourceMutation.SynchronizeAsync(
                    new[] { user.Id },
                    this.imageRepository,
                    this.userRepository,
                    CancellationToken.None);
            }

            return updatedUser;
        }
        finally
        {
            await this.shareSourceRevisionGuard.CompleteMutationAsync(
                mutationLease,
                sourceMutationAttempted,
                CancellationToken.None);
        }
    }

    private async Task<ApplicationResult<AuthenticatedUserResult>> SignInAsync(User user, VerifiedExternalIdentity identity, CancellationToken cancellationToken)
    {
        if (user.IsBlocked)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.UserBlocked());
        }

        if (!user.IsActivated)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.UserNotActivated());
        }

        DateTime expectedUpdatedAtUtc = user.UpdatedAtUtc;
        PersonalRankingShareIdentityState previousIdentity =
            PersonalRankingShareIdentityState.Capture(user);
        ShareSourceMutationLease? mutationLease = CanChangePublicIdentity(user, identity)
            ? await this.shareSourceRevisionGuard.BeginMutationAsync(
                user.Id,
                cancellationToken)
            : null;
        using CancellationTokenSource mutationCancellation =
            ShareSourceMutationCancellation.CreateLinkedSource(
                cancellationToken,
                mutationLease);
        bool sourceMutationAttempted = false;
        bool avatarImported = false;
        User? updatedUser;
        try
        {
            ApplyIdentityToUser(user, identity, false);
            await this.EnsurePublicIdentityAsync(user, mutationCancellation.Token);

            if (ShouldImportAvatar(user, identity))
            {
                sourceMutationAttempted = mutationLease is not null;
                string avatarPath = await this.userAvatarImporter.DownloadAndSaveAsync(
                    identity.PictureUrl!,
                    user.Id,
                    mutationCancellation.Token);
                if (!string.IsNullOrWhiteSpace(avatarPath))
                {
                    user.AvatarUrl = avatarPath;
                    avatarImported = true;
                }
            }

            user.LastLoginUtc = DateTime.UtcNow;
            user.LastActivityUtc = user.LastLoginUtc;
            user.UpdatedAtUtc = user.LastLoginUtc;

            sourceMutationAttempted |= previousIdentity !=
                PersonalRankingShareIdentityState.Capture(user);
            updatedUser = await this.userRepository.UpdateIfUnchangedAsync(
                user.Id,
                user,
                expectedUpdatedAtUtc,
                mutationCancellation.Token);
            if (updatedUser is null && avatarImported)
            {
                await UserAvatarShareSourceMutation.SynchronizeAsync(
                    new[] { user.Id },
                    this.imageRepository,
                    this.userRepository,
                    CancellationToken.None);
            }
        }
        finally
        {
            await this.shareSourceRevisionGuard.CompleteMutationAsync(
                mutationLease,
                sourceMutationAttempted,
                CancellationToken.None);
        }

        if (updatedUser is null)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.UserUpdateFailed());
        }

        DateTime now = DateTime.UtcNow;
        string accessToken = this.tokenService.GenerateUserToken(updatedUser);
        string refreshToken = this.refreshTokenFactory.Generate();
        DateTime refreshTokenExpiresAtUtc = now.AddMinutes(this.authenticationSettings.TokenRefreshLimitMinutes);

        await this.refreshTokenRepository.CreateAsync(
            new RefreshToken
            {
                UserId = updatedUser.Id,
                TokenHash = this.refreshTokenFactory.ComputeHash(refreshToken),
                ExpiresAtUtc = refreshTokenExpiresAtUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            },
            cancellationToken);

        return ApplicationResult<AuthenticatedUserResult>.Success(new AuthenticatedUserResult
        {
            User = updatedUser,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
        });
    }

    private async Task EnsurePublicIdentityAsync(User user, CancellationToken cancellationToken)
    {
        if (user.PublicAccountNumber <= 0)
        {
            long publicAccountNumber =
                await this.userRepository.AllocatePublicAccountNumberAsync(cancellationToken);
            user.AssignPublicAccountNumber(publicAccountNumber);
        }

        if (user.UsesAutomaticPublicDisplayName || string.IsNullOrWhiteSpace(user.PublicDisplayName))
        {
            user.PublicDisplayName = PublicDisplayNameFactory.Create(user.Roles, user.PublicAccountNumber);
            user.UsesAutomaticPublicDisplayName = true;
        }
    }

    private static bool CanChangePublicIdentity(
        User user,
        VerifiedExternalIdentity identity)
    {
        return user.PublicAccountNumber <= 0
            || user.UsesAutomaticPublicDisplayName
            || string.IsNullOrWhiteSpace(user.PublicDisplayName)
            || ShouldImportAvatar(user, identity);
    }

    private static bool ShouldImportAvatar(
        User user,
        VerifiedExternalIdentity identity)
    {
        return string.IsNullOrWhiteSpace(user.AvatarUrl)
            && !string.IsNullOrWhiteSpace(identity.PictureUrl);
    }

    private static bool CanAutoLink(User existingUser, VerifiedExternalIdentity identity)
    {
        bool isLegacySocialAccount = string.IsNullOrWhiteSpace(existingUser.HashedPassword)
                                     && (existingUser.ExternalLogins == null || existingUser.ExternalLogins.Count == 0);

        if (isLegacySocialAccount)
        {
            return true;
        }

        return identity.IsEmailAuthoritative;
    }
}
