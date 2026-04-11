using System.Linq;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Users.Contracts;
using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Users;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Mappers HTTP de la feature Users.
/// </summary>
public static class UsersHttpMappers
{
    public static RegisterUserRequest ToApplication(this UserCreateDto request)
    {
        return new RegisterUserRequest
        {
            Email = request.Email,
            Password = request.Password,
            VerifyPassword = request.VerifyPassword,
            PreferredLanguage = request.PreferredLanguage,
        };
    }

    public static LoginRequest ToApplication(this UserLoginDto request)
    {
        return new LoginRequest
        {
            Email = request.Email,
            Password = request.Password,
        };
    }

    public static RefreshTokenRequest ToApplication(this RefreshTokenRequestDto? request)
    {
        return new RefreshTokenRequest
        {
            RefreshToken = request?.RefreshToken ?? string.Empty,
        };
    }

    public static UserProfileUpdate ToApplication(this UserUpdateDto request)
    {
        return new UserProfileUpdate
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            NewEmail = request.NewEmail,
            PreferredLanguage = request.PreferredLanguage,
            AvatarUrl = request.AvatarUrl,
        };
    }

    public static ChangePasswordRequest ToApplication(this ChangePasswordDto request)
    {
        return new ChangePasswordRequest
        {
            CurrentPassword = request.ActualPassword,
            NewPassword = request.NewPassword,
            VerifyNewPassword = request.NewPasswordConfirm,
        };
    }

    public static ForgotPasswordRequest ToApplication(this ForgotPasswordDto request)
    {
        return new ForgotPasswordRequest
        {
            Email = request.Email,
        };
    }

    public static ResetPasswordRequest ToApplication(this ResetPasswordDto request)
    {
        return new ResetPasswordRequest
        {
            Token = request.Token,
            NewPassword = request.NewPassword,
            VerifyNewPassword = request.NewPasswordConfirm,
        };
    }

    public static PagedQuery ToApplication(this (int Page, int Size) paging)
    {
        return new PagedQuery(paging.Page, paging.Size);
    }

    public static UserCreatedDto ToCreatedDto(this User user)
    {
        return new UserCreatedDto
        {
            Id = user.Id,
            CreatedAt = user.CreatedAtUtc,
            UpdatedAt = user.UpdatedAtUtc,
            Email = user.Email,
            IsActivated = user.IsActivated,
            IsBlocked = user.IsBlocked,
            Roles = user.Roles.Select(ToHttp).ToList(),
            PreferredLanguage = user.PreferredLanguage,
            AvatarUrl = user.AvatarUrl,
        };
    }

    public static UserGettedDto ToGettedDto(this User user)
    {
        return new UserGettedDto
        {
            Id = user.Id,
            CreatedAt = user.CreatedAtUtc,
            UpdatedAt = user.UpdatedAtUtc,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActivated = user.IsActivated,
            IsBlocked = user.IsBlocked,
            Roles = user.Roles.Select(ToHttp).ToList(),
            PreferredLanguage = user.PreferredLanguage,
            AvatarUrl = user.AvatarUrl,
        };
    }

    public static UserUpdatedDto ToUpdatedDto(this User user)
    {
        return new UserUpdatedDto
        {
            Id = user.Id,
            CreatedAt = user.CreatedAtUtc,
            UpdatedAt = user.UpdatedAtUtc,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsActivated = user.IsActivated,
            IsBlocked = user.IsBlocked,
            Roles = user.Roles.Select(ToHttp).ToList(),
            PreferredLanguage = user.PreferredLanguage,
            AvatarUrl = user.AvatarUrl,
        };
    }

    public static UserDto ToListDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            CreatedAt = user.CreatedAtUtc,
            UpdatedAt = user.UpdatedAtUtc,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsActivated = user.IsActivated,
            IsBlocked = user.IsBlocked,
            PreferredLanguage = user.PreferredLanguage,
            Roles = user.Roles.Select(ToHttp).ToList(),
            AvatarUrl = user.AvatarUrl,
        };
    }

    public static PagedResponseDto<UserDto> ToHttp(this PagedResult<User> page)
    {
        return new PagedResponseDto<UserDto>
        {
            Data = page.Items.Select(ToListDto).ToList(),
            Pagination = new PaginationDto
            {
                TotalItems = (int)page.TotalItems,
                TotalPages = page.TotalPages,
                CurrentPage = page.Page,
                ItemsPerPage = page.PageSize,
            },
        };
    }

    public static RoleAssignedDto ToAssignedDto(this User user)
    {
        return new RoleAssignedDto
        {
            UserId = user.Id,
            Roles = user.Roles.Select(ToHttp).ToList(),
        };
    }

    public static RoleRemovedDto ToRemovedDto(this User user)
    {
        return new RoleRemovedDto
        {
            UserId = user.Id,
            Roles = user.Roles.Select(ToHttp).ToList(),
        };
    }

    public static UserLockedDto ToLockedDto(this User user)
    {
        return new UserLockedDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
    }

    public static UserUnlockedDto ToUnlockedDto(this User user)
    {
        return new UserUnlockedDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
    }

    public static UserRoleDto ToHttp(this Role role)
    {
        return role switch
        {
            Role.Admin => UserRoleDto.ADMIN,
            Role.Moderator => UserRoleDto.MODERATOR,
            _ => UserRoleDto.USER,
        };
    }

    public static Role ToDomain(this UserRoleDto role)
    {
        return role switch
        {
            UserRoleDto.ADMIN => Role.Admin,
            UserRoleDto.MODERATOR => Role.Moderator,
            _ => Role.User,
        };
    }
}
