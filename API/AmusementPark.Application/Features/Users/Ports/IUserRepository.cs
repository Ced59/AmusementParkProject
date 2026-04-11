using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Ports;

/// <summary>
/// Port applicatif de persistance des utilisateurs.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken);
    Task<User?> GetByExternalLoginAsync(ExternalLoginProvider provider, string providerUserId, CancellationToken cancellationToken);
    Task<User?> GetByEmailConfirmationTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<User?> GetByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<PagedResult<User>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken);
    Task<User?> UpdateAsync(string userId, User user, CancellationToken cancellationToken);
    Task UpdateLastLoginAndActivityAsync(string userId, CancellationToken cancellationToken);
    Task UpdateLastActivityAsync(string userId, CancellationToken cancellationToken);
    Task<User?> AssignRoleAsync(string userId, Role role, CancellationToken cancellationToken);
    Task<User?> RemoveRoleAsync(string userId, Role role, CancellationToken cancellationToken);
    Task<User?> LockAsync(string userId, CancellationToken cancellationToken);
    Task<User?> UnlockAsync(string userId, CancellationToken cancellationToken);
    Task<User?> ConfirmEmailAsync(string token, CancellationToken cancellationToken);
    Task<bool> ResendConfirmationEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(string token, string newPasswordHash, CancellationToken cancellationToken);
    Task<User?> ChangePasswordAsync(string userId, string newPasswordHash, CancellationToken cancellationToken);
}
