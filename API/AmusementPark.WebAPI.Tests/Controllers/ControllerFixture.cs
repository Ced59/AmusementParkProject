using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.WebAPI.Contracts.Ratings;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

internal sealed record ControllerFixture(
    RatingMethodologiesController Controller,
    Mock<IQueryHandler<GetCurrentRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>>> CurrentHandler,
    Mock<IQueryHandler<GetRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>>> VersionHandler);
