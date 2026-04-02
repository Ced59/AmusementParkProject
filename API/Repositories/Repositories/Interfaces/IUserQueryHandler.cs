using Common.Users;
using Entities.Model.Users;

namespace Repositories.Interfaces
{
    public interface IUserQueryHandler
    {
        Task<User?> GetUserByIdAsync(string id);

        Task<IEnumerable<User>> GetAllUsersAsync();

        Task<User?> CreateUserAsync(User user);

        Task<User?> UpdateUserAsync(User user);

        Task DeleteUserAsync(string id);

        Task<bool> ExistsByEmailAsync(string? email);

        Task<User?> GetUserByEmailAsync(string? email);

        Task<User?> GetUserByExternalLoginAsync(ExternalLoginProvider provider, string providerUserId);

        Task<User?> GetUserByEmailConfirmationTokenHashAsync(string tokenHash);

        Task<User?> GetUserByPasswordResetTokenHashAsync(string tokenHash);

        Task UpdateLastLoginAndActivityAsync(string userId);

        Task UpdateLastActivityAsync(string userId);

        Task<User?> AssignRoleAsync(string userId, Role role);

        Task<User?> RemoveRoleAsync(string userId, Role role);

        Task<User?> LockUser(string userId);

        Task<User?> UnlockUser(string userId);

        Task<IEnumerable<User>> GetUsersPaginatedAsync(int page, int pageSize);

        Task<long> GetTotalUsersCountAsync();

        Task<bool> ChangePassword(string idUser, string newHashedPassword);
    }
}
