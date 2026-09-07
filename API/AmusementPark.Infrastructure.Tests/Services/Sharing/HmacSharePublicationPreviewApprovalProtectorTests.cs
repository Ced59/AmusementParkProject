using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Authentication;
using AmusementPark.Infrastructure.Services.Sharing;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Sharing;

public sealed class HmacSharePublicationPreviewApprovalProtectorTests
{
    private const string OwnerId = "owner-1";
    private const string ScopeKey = "personal-ranking:owner-1";

    [Fact]
    public void IsValid_WhenPolicyWasChangedAfterPreview_ShouldRejectTheApproval()
    {
        HmacSharePublicationPreviewApprovalProtector protector = CreateProtector();
        ShareContentPolicy anonymousPolicy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings });
        string token = protector.CreateToken(
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            12,
            new SharePublicationApprovalState(null, null),
            anonymousPolicy);
        ShareContentPolicy namedPolicy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.GlobalRatings,
            });

        bool isValid = protector.IsValid(
            token,
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            12,
            new SharePublicationApprovalState(null, null),
            namedPolicy);

        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WhenEveryApprovedValueMatches_ShouldAcceptTheApproval()
    {
        HmacSharePublicationPreviewApprovalProtector protector = CreateProtector();
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings });
        string token = protector.CreateToken(
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            12,
            new SharePublicationApprovalState("publication-1", 3),
            policy);

        bool isValid = protector.IsValid(
            token,
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            12,
            new SharePublicationApprovalState("publication-1", 3),
            policy);

        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_WhenPublicationChangedAfterPreview_ShouldRejectTheApproval()
    {
        HmacSharePublicationPreviewApprovalProtector protector = CreateProtector();
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings });
        string token = protector.CreateToken(
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            12,
            new SharePublicationApprovalState("publication-1", 3),
            policy);

        bool isValid = protector.IsValid(
            token,
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            12,
            new SharePublicationApprovalState("publication-1", 4),
            policy);

        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WhenVisitSelectionChangedAfterPreview_ShouldRejectTheApproval()
    {
        HmacSharePublicationPreviewApprovalProtector protector = CreateProtector();
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Month,
            new[] { ShareContentField.RideCount });
        string token = protector.CreateToken(
            OwnerId,
            SharePublicationType.VisitRecap,
            "visit-recap:scope",
            12,
            new SharePublicationApprovalState(null, null),
            policy,
            "selection-a");

        bool isValid = protector.IsValid(
            token,
            OwnerId,
            SharePublicationType.VisitRecap,
            "visit-recap:scope",
            12,
            new SharePublicationApprovalState(null, null),
            policy,
            "selection-b");

        Assert.False(isValid);
    }

    private static HmacSharePublicationPreviewApprovalProtector CreateProtector()
    {
        return new HmacSharePublicationPreviewApprovalProtector(new JwtSettings
        {
            Key = "a-dedicatedly-derived-test-signing-secret-with-enough-entropy",
        });
    }
}
