using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkDataEditorTokens.Commands;
using AmusementPark.Application.Features.ParkDataEditorTokens.Ports;
using AmusementPark.Application.Features.ParkDataEditorTokens.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Handlers;

public sealed class CreateParkDataEditorTokenCommandHandler : ICommandHandler<CreateParkDataEditorTokenCommand, ApplicationResult<CreatedParkDataEditorTokenResult>>
{
    internal const int MaximumActiveTokensPerUser = 3;
    internal const int MaximumLifetimeDays = 90;

    private readonly IUserRepository userRepository;
    private readonly IParkDataEditorAccessTokenRepository tokenRepository;
    private readonly IParkDataEditorTokenProtector tokenProtector;

    public CreateParkDataEditorTokenCommandHandler(
        IUserRepository userRepository,
        IParkDataEditorAccessTokenRepository tokenRepository,
        IParkDataEditorTokenProtector tokenProtector)
    {
        this.userRepository = userRepository;
        this.tokenRepository = tokenRepository;
        this.tokenProtector = tokenProtector;
    }

    public async Task<ApplicationResult<CreatedParkDataEditorTokenResult>> HandleAsync(
        CreateParkDataEditorTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        string label = command.Label?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(command.UserId)
            || label.Length is < 3 or > 80
            || command.ExpiresInDays is < 1 or > MaximumLifetimeDays)
        {
            return ApplicationResult<CreatedParkDataEditorTokenResult>.Failure(
                ParkDataEditorTokenApplicationErrors.InvalidRequest());
        }

        User? user = await this.userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null
            || !user.IsActivated
            || user.IsBlocked
            || !user.HasRole(Role.ParkDataEditor))
        {
            return ApplicationResult<CreatedParkDataEditorTokenResult>.Failure(
                ParkDataEditorTokenApplicationErrors.UserNotEligible());
        }

        DateTime utcNow = DateTime.UtcNow;
        long activeTokenCount = await this.tokenRepository.CountActiveByUserIdAsync(
            user.Id,
            utcNow,
            cancellationToken);
        if (activeTokenCount >= MaximumActiveTokensPerUser)
        {
            return ApplicationResult<CreatedParkDataEditorTokenResult>.Failure(
                ParkDataEditorTokenApplicationErrors.ActiveTokenLimitReached());
        }

        string tokenId = Guid.NewGuid().ToString("N");
        ParkDataEditorTokenMaterial material = this.tokenProtector.Create(tokenId);
        ParkDataEditorAccessToken token = new ParkDataEditorAccessToken
        {
            Id = tokenId,
            UserId = user.Id,
            Label = label,
            TokenHash = material.TokenHash,
            DisplayPrefix = material.DisplayPrefix,
            ExpiresAtUtc = utcNow.AddDays(command.ExpiresInDays),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };

        await this.tokenRepository.CreateAsync(token, cancellationToken);
        CreatedParkDataEditorTokenResult result = new CreatedParkDataEditorTokenResult(
            token,
            material.PlainTextToken);
        return ApplicationResult<CreatedParkDataEditorTokenResult>.Success(result);
    }
}
