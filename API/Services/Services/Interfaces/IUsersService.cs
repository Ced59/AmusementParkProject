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
using Dtos.Users.Login;
using static Entities.Model.Errors.ErrorCodes;
using Dtos.Users.RefreshToken;

namespace Services.Interfaces
{
    public interface IUsersService
    {
        /// <summary>
        /// Create User
        /// </summary>
        /// <param name="user">User to create</param>
        /// <returns>Confirmation created or error</returns>
        Task<OneOf<UserCreatedDto, ErrorCodes.ErrorDetail>>? CreateUserAsync(UserCreateDto user);

        /// <summary>
        /// Get user by email
        /// </summary>
        /// <param name="email">Email of user</param>
        /// <returns>User or error</returns>
        Task<OneOf<UserCreatedDto, ErrorCodes.ErrorDetail>> GetUserByEmailAsync(string email);

        /// <summary>
        /// Get user by id
        /// </summary>
        /// <param name="id">Id of user</param>
        /// <returns>User or error</returns>
        Task<OneOf<UserCreatedDto, ErrorCodes.ErrorDetail>> GetUserByIdAsync(string id);

        /// <summary>
        /// Update user
        /// </summary>
        /// <param name="id">Id of user</param>
        /// <param name="userUpdate">User updated</param>
        /// <returns>User updated or error</returns>
        Task<OneOf<UserUpdatedDto, ErrorCodes.ErrorDetail>> UpdateUser(string id, UserUpdateDto userUpdate);

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="credentials">Credentials of users</param>
        /// <returns>Jwt Token or error</returns>
        Task<OneOf<UserLoggedDto, ErrorDetail>> LoginAsync(UserLoginDto credentials);

        /// <summary>
        /// Refresh Token
        /// </summary>
        /// <param name="token">Token to refresh</param>
        /// <returns>Jwt Token refreshed</returns>
        Task<OneOf<RefreshTokenResponseDto, ErrorDetail>> RefreshTokenAsync(RefreshTokenRequestDto token);
    }
}
