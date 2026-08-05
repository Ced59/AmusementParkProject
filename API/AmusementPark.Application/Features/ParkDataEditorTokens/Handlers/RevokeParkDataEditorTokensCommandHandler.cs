using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkDataEditorTokens.Commands;
using AmusementPark.Application.Features.ParkDataEditorTokens.Ports;
using AmusementPark.Application.Features.ParkDataEditorTokens.Results;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Handlers;

public sealed class RevokeParkDataEditorTokensCommandHandler : ICommandHandler<RevokeParkDataEditorTokensCommand, ApplicationResult<RevokedParkDataEditorTokensResult>>
{
    private readonly IParkDataEditorAccessTokenRepository tokenRepository;

    public RevokeParkDataEditorTokensCommandHandler(IParkDataEditorAccessTokenRepository tokenRepository)
    {
        this.tokenRepository = tokenRepository;
    }

    public async Task<ApplicationResult<RevokedParkDataEditorTokensResult>> HandleAsync(
        RevokeParkDataEditorTokensCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId)
            || string.IsNullOrWhiteSpace(command.RevokedByUserId)
            || string.IsNullOrWhiteSpace(command.Reason))
        {
            return ApplicationResult<RevokedParkDataEditorTokensResult>.Failure(
                ParkDataEditorTokenApplicationErrors.InvalidRequest());
        }

        long revokedCount = await this.tokenRepository.RevokeAsync(
            command.UserId,
            string.IsNullOrWhiteSpace(command.TokenId) ? null : command.TokenId.Trim(),
            command.RevokedByUserId,
            command.Reason.Trim(),
            DateTime.UtcNow,
            cancellationToken);
        if (revokedCount == 0 && !string.IsNullOrWhiteSpace(command.TokenId))
        {
            return ApplicationResult<RevokedParkDataEditorTokensResult>.Failure(
                ParkDataEditorTokenApplicationErrors.TokenNotFound());
        }

        return ApplicationResult<RevokedParkDataEditorTokensResult>.Success(
            new RevokedParkDataEditorTokensResult(revokedCount));
    }
}
