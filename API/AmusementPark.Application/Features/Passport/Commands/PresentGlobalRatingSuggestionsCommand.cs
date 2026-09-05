using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record PresentGlobalRatingSuggestionsCommand(
    string UserId,
    IReadOnlyCollection<GlobalRatingSuggestionTargetKey> Targets)
    : ICommand<ApplicationResult<GlobalRatingSuggestionPresentationResult>>;
