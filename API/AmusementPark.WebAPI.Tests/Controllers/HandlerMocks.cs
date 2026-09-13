using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Passport;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

internal sealed record HandlerMocks(
    Mock<IQueryHandler<
        GetGlobalRatingSuggestionsQuery,
        ApplicationResult<GlobalRatingSuggestionsResult>>> Query,
    Mock<ICommandHandler<
        SetGlobalRatingSuggestionsEnabledCommand,
        ApplicationResult<GlobalRatingSuggestionPreferenceResult>>> Preference,
    Mock<ICommandHandler<
        RecordGlobalRatingSuggestionInteractionCommand,
        ApplicationResult<GlobalRatingSuggestionPreferenceResult>>> Interaction,
    Mock<ICommandHandler<
        PresentGlobalRatingSuggestionsCommand,
        ApplicationResult<GlobalRatingSuggestionPresentationResult>>> Presentation);
