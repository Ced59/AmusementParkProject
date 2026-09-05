using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record SetGlobalRatingSuggestionsEnabledCommand(
    string UserId,
    bool IsEnabled)
    : ICommand<ApplicationResult<GlobalRatingSuggestionPreferenceResult>>;
