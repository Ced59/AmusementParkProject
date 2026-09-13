using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class ShareModerationHttpMapperTests
{
    [Fact]
    public void TryToCommand_ShouldMapDefinedPublicReportValuesCaseInsensitively()
    {
        SubmitShareModerationReportRequestDto request = new SubmitShareModerationReportRequestDto
        {
            TargetType = "visitrecap",
            ShareId = "opaque-share",
            Reason = "personaldata",
            Details = "A phone number is visible.",
        };

        bool mapped = request.TryToCommand(out SubmitShareModerationReportCommand? command);

        Assert.True(mapped);
        Assert.NotNull(command);
        Assert.Equal(ShareModerationTargetType.VisitRecap, command.TargetType);
        Assert.Equal(ShareModerationReason.PersonalData, command.Reason);
    }

    [Fact]
    public void TryToCriteria_ShouldRejectUnknownEnumValues()
    {
        ShareModerationReportSearchRequestDto request =
            new ShareModerationReportSearchRequestDto { Status = "Unknown" };

        bool mapped = request.TryToCriteria(out ShareModerationReportSearchCriteria? criteria);

        Assert.False(mapped);
        Assert.Null(criteria);
    }

    [Fact]
    public void ToHttp_ShouldNeverExposeTargetRecordOrReviewerIdentifiers()
    {
        ShareModerationReportResult result = new ShareModerationReportResult(
            "report-1",
            ShareModerationTargetType.PassportProfile,
            ShareModerationReason.Impersonation,
            "This profile uses my name.",
            ShareModerationReportStatus.PublicationSuspended,
            new DateTime(2026, 9, 13, 18, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 13, 18, 5, 0, DateTimeKind.Utc),
            "Confirmed.");

        ShareModerationReportDto dto = result.ToHttp();

        Assert.Equal("PassportProfile", dto.TargetType);
        Assert.DoesNotContain(
            dto.GetType().GetProperties(),
            static property => property.Name.Contains("TargetRecord", StringComparison.Ordinal)
                || property.Name.Contains("Reviewer", StringComparison.Ordinal)
                || property.Name.Contains("ShareId", StringComparison.Ordinal));
    }
}
