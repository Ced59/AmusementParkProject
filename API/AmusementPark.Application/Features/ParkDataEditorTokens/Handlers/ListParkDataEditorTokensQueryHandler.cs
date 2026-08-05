using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkDataEditorTokens.Ports;
using AmusementPark.Application.Features.ParkDataEditorTokens.Queries;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Handlers;

public sealed class ListParkDataEditorTokensQueryHandler : IQueryHandler<ListParkDataEditorTokensQuery, ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>>>
{
    private readonly IUserRepository userRepository;
    private readonly IParkDataEditorAccessTokenRepository tokenRepository;

    public ListParkDataEditorTokensQueryHandler(
        IUserRepository userRepository,
        IParkDataEditorAccessTokenRepository tokenRepository)
    {
        this.userRepository = userRepository;
        this.tokenRepository = tokenRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>>> HandleAsync(
        ListParkDataEditorTokensQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>>.Failure(
                ParkDataEditorTokenApplicationErrors.InvalidRequest());
        }

        User? user = await this.userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>>.Failure(
                ParkDataEditorTokenApplicationErrors.UserNotEligible());
        }

        IReadOnlyCollection<ParkDataEditorAccessToken> tokens =
            await this.tokenRepository.GetByUserIdAsync(user.Id, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>>.Success(tokens);
    }
}
