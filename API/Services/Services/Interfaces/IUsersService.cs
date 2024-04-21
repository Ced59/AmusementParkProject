using Entities.Model.Errors;
using Entities.Model.Users;
using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dtos.Users.Creating;
using Dtos.Users.Updating;

namespace Services.Interfaces
{
    public interface IUsersService
    {
        Task<OneOf<UserCreatedDto, ErrorCodes.ErrorDetail>>? CreateUserAsync(UserCreateDto user);
        Task<OneOf<UserCreatedDto, ErrorCodes.ErrorDetail>> GetUserByEmailAsync(string email);
        Task<OneOf<UserCreatedDto, ErrorCodes.ErrorDetail>> GetUserByIdAsync(string id);
        Task<OneOf<UserUpdatedDto, ErrorCodes.ErrorDetail>> UpdateUser(string id, UserUpdateDto userUpdate);
    }
}
