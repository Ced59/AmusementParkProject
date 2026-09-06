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
            policy);

        bool isValid = protector.IsValid(
            token,
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            12,
            policy);

        Assert.True(isValid);
    }

    private static HmacSharePublicationPreviewApprovalProtector CreateProtector()
    {
        return new HmacSharePublicationPreviewApprovalProtector(new JwtSettings
        {
            Key = "a-dedicatedly-derived-test-signing-secret-with-enough-entropy",
        });
    }
}
