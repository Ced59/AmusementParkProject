using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Queries;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de pagination des utilisateurs.
/// </summary>
public sealed class GetUsersPageQueryHandler : IQueryHandler<GetUsersPageQuery, ApplicationResult<PagedResult<User>>>
{
    private readonly IUserRepository userRepository;

    public GetUsersPageQueryHandler(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<PagedResult<User>>> HandleAsync(GetUsersPageQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Paging.Page <= 0 || query.Paging.PageSize <= 0)
        {
            return ApplicationResult<PagedResult<User>>.Failure(ApplicationErrors.InvalidPagination());
        }

        PagedResult<User> page = await this.userRepository.GetPageAsync(query.Paging.Page, query.Paging.PageSize, cancellationToken);
        return ApplicationResult<PagedResult<User>>.Success(page);
    }
}
