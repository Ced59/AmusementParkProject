using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class SetGlobalRatingSuggestionsEnabledCommandHandler
    : ICommandHandler<
        SetGlobalRatingSuggestionsEnabledCommand,
        ApplicationResult<GlobalRatingSuggestionPreferenceResult>>
{
    private readonly IGlobalRatingSuggestionStateRepository stateRepository;
    private readonly IGlobalRatingSuggestionFeatureGate featureGate;
    private readonly IPassportClock clock;

    public SetGlobalRatingSuggestionsEnabledCommandHandler(
        IGlobalRatingSuggestionStateRepository stateRepository,
        IGlobalRatingSuggestionFeatureGate featureGate,
        IPassportClock clock)
    {
        this.stateRepository = stateRepository;
        this.featureGate = featureGate;
        this.clock = clock;
    }

    public async Task<ApplicationResult<GlobalRatingSuggestionPreferenceResult>> HandleAsync(
        SetGlobalRatingSuggestionsEnabledCommand command,
        CancellationToken cancellationToken = default)
    {
        string userId;
        try
        {
            userId = IdentifierRules.NormalizeRequired(command.UserId, nameof(command.UserId));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        await this.stateRepository.SetEnabledAsync(
            userId,
            command.IsEnabled,
            this.clock.UtcNow,
            cancellationToken);
        return ApplicationResult<GlobalRatingSuggestionPreferenceResult>.Success(
            new GlobalRatingSuggestionPreferenceResult(
                this.featureGate.IsEnabled,
                command.IsEnabled));
    }
}
