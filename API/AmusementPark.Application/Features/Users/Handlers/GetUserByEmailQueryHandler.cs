using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Queries;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de récupération d'un utilisateur par email.
/// </summary>
public sealed class GetUserByEmailQueryHandler : IQueryHandler<GetUserByEmailQuery, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;

    public GetUserByEmailQueryHandler(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<User>> HandleAsync(GetUserByEmailQuery query, CancellationToken cancellationToken = default)
    {
        string? normalizedEmail = UserRules.NormalizeEmail(query.Email);
        if (!UserRules.IsValidEmail(normalizedEmail))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.InvalidEmailAddress());
        }

        User? user = await this.userRepository.GetByEmailAsync(normalizedEmail!, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
        }

        return ApplicationResult<User>.Success(user);
    }
}
