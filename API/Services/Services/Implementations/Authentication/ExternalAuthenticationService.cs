using Common.Users;
using Dtos.Users.Login;
using Entities.Model.Errors;
using Entities.Model.Users;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Interfaces.Authentication;
using Services.Interfaces.Settings;
using Services.Models.Authentication;
using Services.Security;

namespace Services.Implementations.Authentication
{
    public class ExternalAuthenticationService : IExternalAuthenticationService
    {
        private readonly IReadOnlyDictionary<ExternalLoginProvider, IExternalIdentityProviderService> providerServices;
        private readonly IUserQueryHandler userQueryHandler;
        private readonly IUsersService usersService;
        private readonly IJwtSettings jwtSettings;

        public ExternalAuthenticationService(
            IEnumerable<IExternalIdentityProviderService> providerServices,
            IUserQueryHandler userQueryHandler,
            IUsersService usersService,
            IJwtSettings jwtSettings)
        {
            this.providerServices = providerServices.ToDictionary(service => service.Provider);
            this.userQueryHandler = userQueryHandler;
            this.usersService = usersService;
            this.jwtSettings = jwtSettings;
        }

        public async Task<OneOf<UserLoggedDto, ErrorCodes.ErrorDetail>> AuthenticateAsync(
            string provider,
            string token,
            string? nonce,
            CancellationToken cancellationToken = default)
        {
            if (!TryParseProvider(provider, out ExternalLoginProvider externalLoginProvider))
            {
                return ErrorCodes.ExternalAuthenticationProviderNotSupported;
            }

            if (!providerServices.TryGetValue(externalLoginProvider, out IExternalIdentityProviderService? providerService))
            {
                return ErrorCodes.ExternalAuthenticationProviderNotSupported;
            }

            VerifiedExternalIdentity? verifiedIdentity = await providerService.VerifyAsync(token, nonce, cancellationToken);
            if (verifiedIdentity is null)
            {
                return ErrorCodes.InvalidExternalIdentityToken;
            }

            User? userByProvider = await userQueryHandler.GetUserByExternalLoginAsync(verifiedIdentity.Provider, verifiedIdentity.ProviderUserId);
            if (userByProvider is not null)
            {
                return await SignInAsync(userByProvider, verifiedIdentity);
            }

            User? userByEmail = await userQueryHandler.GetUserByEmailAsync(verifiedIdentity.Email);
            if (userByEmail is not null)
            {
                if (!CanAutoLink(userByEmail, verifiedIdentity))
                {
                    return ErrorCodes.ExternalLoginRequiresAccountLinking;
                }

                ApplyIdentityToUser(userByEmail, verifiedIdentity, allowNameBackfill: true);
                User? updatedExistingUser = await PersistUserAsync(userByEmail, verifiedIdentity);
                if (updatedExistingUser is null)
                {
                    return ErrorCodes.UserUpdateFailed;
                }

                return await SignInAsync(updatedExistingUser, verifiedIdentity);
            }

            User newUser = BuildUserFromIdentity(verifiedIdentity);
            User? createdUser = await PersistUserAsync(newUser, verifiedIdentity, createIfMissing: true);
            if (createdUser is null)
            {
                return ErrorCodes.LoginFailed;
            }

            return await SignInAsync(createdUser, verifiedIdentity);
        }

        private static bool TryParseProvider(string provider, out ExternalLoginProvider externalLoginProvider)
        {
            return Enum.TryParse(provider, true, out externalLoginProvider);
        }

        private static User BuildUserFromIdentity(VerifiedExternalIdentity identity)
        {
            DateTime now = DateTime.UtcNow;

            User user = new()
            {
                Email = NormalizeEmail(identity.Email),
                CreatedAt = now,
                UpdatedAt = now,
                PreferredLanguage = "EN",
                IsActivated = true,
                IsBlocked = false,
                FirstName = identity.GivenName,
                LastName = identity.FamilyName,
                Roles = new List<Role>
                {
                    Role.USER
                },
                HashedPassword = string.Empty,
                LastLogin = now,
                LastActivity = now
            };

            ApplyIdentityToUser(user, identity, allowNameBackfill: true);
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
                    LinkedAt = DateTime.UtcNow
                };
                user.ExternalLogins.Add(existingLogin);
            }

            existingLogin.ProviderUserId = identity.ProviderUserId;
            existingLogin.Email = NormalizeEmail(identity.Email);
            existingLogin.IsEmailVerified = identity.IsEmailVerified;
            existingLogin.DisplayName = identity.DisplayName;
            existingLogin.GivenName = identity.GivenName;
            existingLogin.FamilyName = identity.FamilyName;
            existingLogin.PictureUrl = identity.PictureUrl;
            existingLogin.HostedDomain = identity.HostedDomain;
            existingLogin.LastLoginAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                user.Email = NormalizeEmail(identity.Email);
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

        private async Task<User?> PersistUserAsync(User user, VerifiedExternalIdentity identity, bool createIfMissing = false)
        {
            if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(identity.PictureUrl))
            {
                string avatarPath = await usersService.DownloadAndSaveUserAvatar(identity.PictureUrl, user.Id);
                if (!string.IsNullOrWhiteSpace(avatarPath))
                {
                    user.AvatarUrl = avatarPath;
                }
            }

            user.UpdatedAt = DateTime.UtcNow;

            if (createIfMissing)
            {
                return await userQueryHandler.CreateUserAsync(user);
            }

            return await userQueryHandler.UpdateUserAsync(user);
        }

        private async Task<OneOf<UserLoggedDto, ErrorCodes.ErrorDetail>> SignInAsync(User user, VerifiedExternalIdentity identity)
        {
            if (user.IsBlocked)
            {
                return ErrorCodes.UserBlocked;
            }

            if (!user.IsActivated)
            {
                return ErrorCodes.UserNotActivated;
            }

            ApplyIdentityToUser(user, identity, allowNameBackfill: false);

            if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(identity.PictureUrl))
            {
                string avatarPath = await usersService.DownloadAndSaveUserAvatar(identity.PictureUrl, user.Id);
                if (!string.IsNullOrWhiteSpace(avatarPath))
                {
                    user.AvatarUrl = avatarPath;
                }
            }

            user.LastLogin = DateTime.UtcNow;
            user.LastActivity = user.LastLogin;
            user.UpdatedAt = user.LastLogin;

            User? updatedUser = await userQueryHandler.UpdateUserAsync(user);
            if (updatedUser is null)
            {
                return ErrorCodes.UserUpdateFailed;
            }

            string token = JwtHelper.GenerateToken(updatedUser, jwtSettings);
            return new UserLoggedDto
            {
                Token = token
            };
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

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}
