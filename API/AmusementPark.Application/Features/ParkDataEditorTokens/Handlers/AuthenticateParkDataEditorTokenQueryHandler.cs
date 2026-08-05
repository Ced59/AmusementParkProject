using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkDataEditorTokens.Ports;
using AmusementPark.Application.Features.ParkDataEditorTokens.Queries;
using AmusementPark.Application.Features.ParkDataEditorTokens.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Handlers;

public sealed class AuthenticateParkDataEditorTokenQueryHandler : IQueryHandler<AuthenticateParkDataEditorTokenQuery, ApplicationResult<ParkDataEditorTokenAuthenticationResult>>
{
    private static readonly TimeSpan LastUsedWriteInterval = TimeSpan.FromMinutes(5);
    private readonly IUserRepository userRepository;
    private readonly IParkDataEditorAccessTokenRepository tokenRepository;
    private readonly IParkDataEditorTokenProtector tokenProtector;

    public AuthenticateParkDataEditorTokenQueryHandler(
        IUserRepository userRepository,
        IParkDataEditorAccessTokenRepository tokenRepository,
        IParkDataEditorTokenProtector tokenProtector)
    {
        this.userRepository = userRepository;
        this.tokenRepository = tokenRepository;
        this.tokenProtector = tokenProtector;
    }

    public async Task<ApplicationResult<ParkDataEditorTokenAuthenticationResult>> HandleAsync(
        AuthenticateParkDataEditorTokenQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!this.tokenProtector.TryReadTokenId(query.PlainTextToken, out string tokenId))
        {
            return InvalidTokenResult();
        }

        ParkDataEditorAccessToken? token = await this.tokenRepository.GetByIdAsync(tokenId, cancellationToken);
        DateTime utcNow = DateTime.UtcNow;
        if (token is null
            || !token.IsActiveAt(utcNow)
            || !this.tokenProtector.Verify(query.PlainTextToken, token))
        {
            return InvalidTokenResult();
        }

        User? user = await this.userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null
            || !user.IsActivated
            || user.IsBlocked
            || !user.HasRole(Role.ParkDataEditor))
        {
            return InvalidTokenResult();
        }

        await this.tokenRepository.MarkUsedAsync(
            token.Id,
            utcNow,
            utcNow.Subtract(LastUsedWriteInterval),
            cancellationToken);

        ParkDataEditorTokenAuthenticationResult result = new ParkDataEditorTokenAuthenticationResult(user, token);
        return ApplicationResult<ParkDataEditorTokenAuthenticationResult>.Success(result);
    }

    private static ApplicationResult<ParkDataEditorTokenAuthenticationResult> InvalidTokenResult()
    {
        return ApplicationResult<ParkDataEditorTokenAuthenticationResult>.Failure(
            ParkDataEditorTokenApplicationErrors.InvalidToken());
    }
}
