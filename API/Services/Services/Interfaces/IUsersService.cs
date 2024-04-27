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
using Dtos.Users.Roles;
using Dtos.Users.LockUser;
using Dtos.Users.UserGet;
using Dtos.Pagination;

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
        Task<OneOf<UserGettedDto, ErrorCodes.ErrorDetail>> GetUserByEmailAsync(string email);

        /// <summary>
        /// Get user by id
        /// </summary>
        /// <param name="id">Id of user</param>
        /// <returns>User or error</returns>
        Task<OneOf<UserGettedDto, ErrorCodes.ErrorDetail>> GetUserByIdAsync(string id);

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

        /// <summary>
        /// Assign role to User
        /// </summary>
        /// <param name="userId">Id user</param>
        /// <param name="roleAssignDto">Role to assign</param>
        /// <returns>All roles of user or error</returns>
        Task<OneOf<RoleAssignedDto, ErrorDetail>> AssignRoleAsync(string userId, RoleAssignDto roleAssignDto);

        /// <summary>
        /// Remove role from User
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <param name="roleToRemove">Role to remove</param>
        /// <returns>All roles of user or error</returns>
        Task<OneOf<RoleRemovedDto, ErrorDetail>> RemoveRoleAsync(string userId, RoleRemoveDto roleToRemove);

        /// <summary>
        /// Lock user
        /// </summary>
        /// <param name="userToLock">User to lock</param>
        /// <returns>User locked</returns>
        Task<OneOf<UserLockedDto, ErrorDetail>> LockUser(UserToLockDto userToLock);

        /// <summary>
        /// Unlock user
        /// </summary>
        /// <param name="userToUnlock">User to unlock</param>
        /// <returns>User unlocked</returns>
        Task<OneOf<UserUnlockedDto, ErrorDetail>> UnlockUser(UserToUnlockDto userToUnlock);

        /// <summary>
        /// Get list of users with pagination
        /// </summary>
        /// <param name="page">The page requested</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <returns>List of users with pagination</returns>
        Task<(IEnumerable<User>, PaginationDto)> GetAllUsersPaginatedAsync(int page, int pageSize);
    }
}
