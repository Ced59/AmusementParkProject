using Entities.Model.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        Task UpdateLastLoginAndActivityAsync(string userId);
        Task UpdateLastActivityAsync(string userId);
    }

}
