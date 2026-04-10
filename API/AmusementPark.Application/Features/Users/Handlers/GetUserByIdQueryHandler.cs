using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Queries;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de récupération d'un utilisateur par identifiant.
/// </summary>
public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<User>> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
        }

        User? user = await this.userRepository.GetByIdAsync(query.UserId.Trim(), cancellationToken);
        if (user is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
        }

        return ApplicationResult<User>.Success(user);
    }
}
