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
        Task<UserInDb> GetUserByIdAsync(string id);
        Task<IEnumerable<UserInDb>> GetAllUsersAsync();
        Task<UserInDb> CreateUserAsync(UserInDb user);
        Task UpdateUserAsync(UserInDb user);
        Task DeleteUserAsync(string id);
        Task<bool> ExistsByEmailAsync(string? email);
        Task<UserInDb> GetUserByEmailAsync(string? email);
    }

}
