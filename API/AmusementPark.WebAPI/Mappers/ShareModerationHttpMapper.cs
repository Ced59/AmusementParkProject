using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Contracts.Sharing;

namespace AmusementPark.WebAPI.Mappers;

public static class ShareModerationHttpMapper
{
    public static bool TryToCommand(
        this SubmitShareModerationReportRequestDto request,
        out SubmitShareModerationReportCommand? command)
    {
        ArgumentNullException.ThrowIfNull(request);
        command = null;
        if (!TryParseDefined(request.TargetType, out ShareModerationTargetType targetType)
            || !TryParseDefined(request.Reason, out ShareModerationReason reason))
        {
            return false;
        }

        command = new SubmitShareModerationReportCommand(
            targetType,
            request.ShareId,
            reason,
            request.Details);
        return true;
    }

    public static bool TryToCommand(
        this ReviewShareModerationReportRequestDto request,
        string reviewerUserId,
        string reportId,
        out ReviewShareModerationReportCommand? command)
    {
        ArgumentNullException.ThrowIfNull(request);
        command = null;
        if (!TryParseDefined(request.Decision, out ShareModerationDecision decision))
        {
            return false;
        }

        command = new ReviewShareModerationReportCommand(
            reviewerUserId,
            reportId,
            decision,
            request.Note);
        return true;
    }

    public static bool TryToCriteria(
        this ShareModerationReportSearchRequestDto request,
        out ShareModerationReportSearchCriteria? criteria)
    {
        ArgumentNullException.ThrowIfNull(request);
        criteria = null;
        if (!TryParseOptional(request.Status, out ShareModerationReportStatus? status)
            || !TryParseOptional(request.TargetType, out ShareModerationTargetType? targetType)
            || !TryParseOptional(request.Reason, out ShareModerationReason? reason))
        {
            return false;
        }

        criteria = new ShareModerationReportSearchCriteria(
            new PagedQuery(request.Page, request.Size),
            status,
            targetType,
            reason);
        return true;
    }

    public static ShareModerationReportDto ToHttp(this ShareModerationReportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ShareModerationReportDto
        {
            ReportId = result.ReportId,
            TargetType = result.TargetType.ToString(),
            Reason = result.Reason.ToString(),
            Details = result.Details,
            Status = result.Status.ToString(),
            SubmittedAtUtc = result.SubmittedAtUtc,
            ReviewedAtUtc = result.ReviewedAtUtc,
            DecisionNote = result.DecisionNote,
        };
    }

    private static bool TryParseOptional<TEnum>(string? value, out TEnum? parsed)
        where TEnum : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!TryParseDefined(value, out TEnum defined))
        {
            return false;
        }

        parsed = defined;
        return true;
    }

    private static bool TryParseDefined<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value?.Trim(), true, out parsed) && Enum.IsDefined(parsed);
    }
}
