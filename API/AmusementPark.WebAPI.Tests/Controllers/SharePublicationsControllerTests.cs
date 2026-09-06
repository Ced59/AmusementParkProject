using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class SharePublicationsControllerTests
{
    [Fact]
    public async Task PreviewAsync_ShouldUseAuthenticatedOwnerAndReturnPublicContract()
    {
        SharePublicationPreviewResult preview = new SharePublicationPreviewResult(
            SharePublicationType.PersonalRanking,
            8,
            1,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings },
            new PersonalRankingSharePreviewResult(
                null,
                null,
                null,
                Array.Empty<PersonalRankingSharePreviewItemResult>(),
                false),
            "approved-preview");
        Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>> handler =
            new Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<PreviewSharePublicationQuery>(query =>
                    query.OwnerUserId == "owner-1"
                    && query.PublicationType == SharePublicationType.PersonalRanking
                    && query.IncludedFields.SequenceEqual(new[] { ShareContentField.GlobalRatings })),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharePublicationPreviewResult>.Success(preview));
        SharePublicationsController controller = CreateController(handler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.PreviewAsync(
            new SharePublicationPreviewRequestDto
            {
                PublicationType = "PersonalRanking",
                DatePrecision = "Hidden",
                IncludedFields = new List<string> { "GlobalRatings" },
            },
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SharePublicationPreviewDto response = Assert.IsType<SharePublicationPreviewDto>(ok.Value);
        Assert.Equal("PersonalRanking", response.PublicationType);
        Assert.Equal(8, response.SourceVersion);
        Assert.Equal("approved-preview", response.ApprovalToken);
        Assert.DoesNotContain(
            response.ContentPolicy.IncludedFields,
            static field => field.Contains("Private", StringComparison.OrdinalIgnoreCase));
        handler.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WhenEnumIsInvalid_ShouldReturnBadRequestWithoutCallingApplication()
    {
        Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>> handler =
            new Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>>(MockBehavior.Strict);
        SharePublicationsController controller = CreateController(handler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.PreviewAsync(
            new SharePublicationPreviewRequestDto
            {
                PublicationType = "InternalSecrets",
                DatePrecision = "Hidden",
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        handler.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("4", "Hidden", "GlobalRatings")]
    [InlineData("PersonalRanking", "0", "GlobalRatings")]
    [InlineData("PersonalRanking", "Hidden", "5")]
    [InlineData("VisitRecap, YearRecap", "Hidden", "GlobalRatings")]
    [InlineData("PersonalRanking", "Hidden", "PublicDisplayName, Avatar")]
    public async Task PreviewAsync_WhenEnumIsNotAnExplicitName_ShouldReturnBadRequestWithoutCallingApplication(
        string publicationType,
        string datePrecision,
        string includedField)
    {
        Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>> handler =
            new Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>>(MockBehavior.Strict);
        SharePublicationsController controller = CreateController(handler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.PreviewAsync(
            new SharePublicationPreviewRequestDto
            {
                PublicationType = publicationType,
                DatePrecision = datePrecision,
                IncludedFields = new List<string> { includedField },
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        handler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PreviewAsync_WhenRollingCandidateIsNotCompatible_ShouldReturnServiceUnavailable()
    {
        Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>> handler =
            new Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>>(MockBehavior.Strict);
        SharePublicationsController controller = CreateController(handler.Object, false);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.PreviewAsync(
            new SharePublicationPreviewRequestDto(),
            CancellationToken.None);

        StatusCodeResult unavailable = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        handler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PublishAsync_ShouldForwardOnlyTheAuthenticatedApprovedPreview()
    {
        SharePublicationSettingsResult settings = new SharePublicationSettingsResult(
            true,
            "opaque-share-id",
            new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc),
            1,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings });
        Mock<ICommandHandler<PublishSharePublicationCommand, ApplicationResult<SharePublicationSettingsResult>>> publishHandler =
            new Mock<ICommandHandler<PublishSharePublicationCommand, ApplicationResult<SharePublicationSettingsResult>>>(MockBehavior.Strict);
        publishHandler.Setup(value => value.HandleAsync(
                It.Is<PublishSharePublicationCommand>(command =>
                    command.UserId == "owner-1"
                    && command.ApprovedSourceVersion == 12
                    && command.ApprovalToken == "approved-preview"
                    && command.ApprovedPolicySchemaVersion == 1
                    && command.ApprovedIncludedFields.SequenceEqual(
                        new[] { ShareContentField.GlobalRatings })),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharePublicationSettingsResult>.Success(settings));
        SharePublicationsController controller = CreateController(
            Mock.Of<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>>(MockBehavior.Strict),
            true,
            publishHandler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.PublishAsync(
            new PublishSharePublicationRequestDto
            {
                PublicationType = "PersonalRanking",
                ApprovedSourceVersion = 12,
                ApprovedPolicySchemaVersion = 1,
                ApprovedDatePrecision = "Hidden",
                ApprovedIncludedFields = new List<string> { "GlobalRatings" },
                ApprovalToken = "approved-preview",
            },
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SharePublicationSettingsDto response = Assert.IsType<SharePublicationSettingsDto>(ok.Value);
        Assert.True(response.IsPublic);
        Assert.Equal("opaque-share-id", response.ShareId);
        Assert.Equal(new[] { "GlobalRatings" }, response.IncludedFields);
        publishHandler.VerifyAll();
    }

    [Fact]
    public void PreviewEndpoint_ShouldRequireAnActivatedAccountDisableCachingAndApplyTargetedRateLimit()
    {
        MethodInfo action = typeof(SharePublicationsController).GetMethod(
            nameof(SharePublicationsController.PreviewAsync))
            ?? throw new InvalidOperationException("Preview action not found.");

        AuthorizeAttribute authorize = Assert.Single(
            action.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.False(string.IsNullOrWhiteSpace(authorize.Roles));
        Assert.NotNull(action.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            action.GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        Assert.Equal(ResponseCacheLocation.None, cache.Location);
        EnableRateLimitingAttribute rateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            action.GetCustomAttribute<EnableRateLimitingAttribute>());
        Assert.Equal(RateLimitPolicyNames.SharePublicationPreviews, rateLimit.PolicyName);
    }

    [Fact]
    public void PublishEndpoint_ShouldRequireConsentSecurityAndInvalidatePublicData()
    {
        MethodInfo action = typeof(SharePublicationsController).GetMethod(
            nameof(SharePublicationsController.PublishAsync))
            ?? throw new InvalidOperationException("Publish action not found.");

        AuthorizeAttribute authorize = Assert.Single(
            action.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.False(string.IsNullOrWhiteSpace(authorize.Roles));
        Assert.NotNull(action.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.NotNull(action.GetCustomAttribute<InvalidatesPublicCacheAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            action.GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        Assert.Equal(ResponseCacheLocation.None, cache.Location);
        EnableRateLimitingAttribute rateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            action.GetCustomAttribute<EnableRateLimitingAttribute>());
        Assert.Equal(RateLimitPolicyNames.SharePublicationPreviews, rateLimit.PolicyName);
    }

    private static ControllerContext CreateControllerContext(string userId)
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "USER"),
            },
            "Test");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };
    }

    private static SharePublicationsController CreateController(
        IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>> handler,
        bool enabled = true,
        ICommandHandler<PublishSharePublicationCommand, ApplicationResult<SharePublicationSettingsResult>>? publishHandler = null)
    {
        return new SharePublicationsController(
            handler,
            publishHandler ?? Mock.Of<ICommandHandler<PublishSharePublicationCommand, ApplicationResult<SharePublicationSettingsResult>>>(MockBehavior.Strict),
            Options.Create(new SharePublicationRolloutSettings { Enabled = enabled }));
    }
}
