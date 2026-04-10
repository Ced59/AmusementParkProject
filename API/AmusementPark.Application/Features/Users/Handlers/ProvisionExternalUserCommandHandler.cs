using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Results;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler d'authentification externe avec provisionnement automatique.
/// </summary>
public sealed class ProvisionExternalUserCommandHandler : ICommandHandler<ProvisionExternalUserCommand, ApplicationResult<AuthenticatedUserResult>>
{
    private readonly IUserRepository userRepository;
    private readonly IExternalIdentityVerifier externalIdentityVerifier;
    private readonly IUserAvatarImporter userAvatarImporter;
    private readonly ITokenService tokenService;

    public ProvisionExternalUserCommandHandler(
        IUserRepository userRepository,
        IExternalIdentityVerifier externalIdentityVerifier,
        IUserAvatarImporter userAvatarImporter,
        ITokenService tokenService)
    {
        this.userRepository = userRepository;
        this.externalIdentityVerifier = externalIdentityVerifier;
        this.userAvatarImporter = userAvatarImporter;
        this.tokenService = tokenService;
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

        User newUser = BuildUserFromIdentity(verifiedIdentity, command.Request.PreferredLanguage);
        User? createdUser = await this.PersistUserAsync(newUser, verifiedIdentity, true, cancellationToken);
        if (createdUser is null)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.LoginFailed());
        }

        return await this.SignInAsync(createdUser, verifiedIdentity, cancellationToken);
    }

    private static User BuildUserFromIdentity(VerifiedExternalIdentity identity, string? preferredLanguage)
    {
        DateTime now = DateTime.UtcNow;

        User user = new User
        {
            Email = UserRules.NormalizeEmail(identity.Email),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PreferredLanguage = UserRules.NormalizePreferredLanguage(preferredLanguage),
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
        if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(identity.PictureUrl))
        {
            string avatarPath = await this.userAvatarImporter.DownloadAndSaveAsync(identity.PictureUrl, user.Id, cancellationToken);
            if (!string.IsNullOrWhiteSpace(avatarPath))
            {
                user.AvatarUrl = avatarPath;
            }
        }

        user.UpdatedAtUtc = DateTime.UtcNow;

        if (createIfMissing)
        {
            return await this.userRepository.CreateAsync(user, cancellationToken);
        }

        return await this.userRepository.UpdateAsync(user.Id, user, cancellationToken);
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

        ApplyIdentityToUser(user, identity, false);

        if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(identity.PictureUrl))
        {
            string avatarPath = await this.userAvatarImporter.DownloadAndSaveAsync(identity.PictureUrl, user.Id, cancellationToken);
            if (!string.IsNullOrWhiteSpace(avatarPath))
            {
                user.AvatarUrl = avatarPath;
            }
        }

        user.LastLoginUtc = DateTime.UtcNow;
        user.LastActivityUtc = user.LastLoginUtc;
        user.UpdatedAtUtc = user.LastLoginUtc;

        User? updatedUser = await this.userRepository.UpdateAsync(user.Id, user, cancellationToken);
        if (updatedUser is null)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.UserUpdateFailed());
        }

        string token = this.tokenService.GenerateUserToken(updatedUser);
        return ApplicationResult<AuthenticatedUserResult>.Success(new AuthenticatedUserResult
        {
            User = updatedUser,
            AccessToken = token,
            RefreshToken = token,
        });
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
