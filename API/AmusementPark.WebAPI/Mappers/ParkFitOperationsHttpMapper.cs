using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.WebAPI.Contracts.ParkFit;

namespace AmusementPark.WebAPI.Mappers;

public static class ParkFitOperationsHttpMapper
{
    public static bool TryToCommand(
        this SubmitParkFitSourceReportRequestDto request,
        out SubmitParkFitSourceReportCommand? command)
    {
        command = null;
        if (!Enum.TryParse(request.EvidenceKind, ignoreCase: false, out ParkFitEvidenceKind evidenceKind)
            || !Enum.IsDefined(evidenceKind)
            || !Enum.TryParse(request.Reason, ignoreCase: false, out ParkFitSourceReportReason reason)
            || !Enum.IsDefined(reason))
        {
            return false;
        }

        command = new SubmitParkFitSourceReportCommand(
            request.ParkId,
            evidenceKind,
            request.SourceUrl,
            request.SourceReference,
            reason,
            request.Details);
        return true;
    }

    public static bool TryToCommand(
        this ReviewParkFitSourceReportRequestDto request,
        string reportId,
        string reviewerUserId,
        out ReviewParkFitSourceReportCommand? command)
    {
        command = null;
        if (!Enum.TryParse(request.Decision, ignoreCase: false, out ParkFitSourceReportStatus decision)
            || !Enum.IsDefined(decision))
        {
            return false;
        }

        command = new ReviewParkFitSourceReportCommand(
            reportId,
            decision,
            reviewerUserId,
            request.DecisionNote,
            request.ExpectedRevision);
        return true;
    }

    public static bool TryToCommand(
        this ChangeParkFitOperationalStatusRequestDto request,
        string parkId,
        string actorUserId,
        out ChangeParkFitOperationalStatusCommand? command)
    {
        command = null;
        if (!Enum.TryParse(request.TargetState, ignoreCase: false, out ParkFitRecommendationState targetState)
            || !Enum.IsDefined(targetState))
        {
            return false;
        }

        command = new ChangeParkFitOperationalStatusCommand(
            parkId,
            targetState,
            actorUserId,
            request.Reason,
            request.ExpectedRevision);
        return true;
    }

    public static bool TryToCriteria(
        this ParkFitSourceReportSearchRequestDto request,
        out ParkFitSourceReportSearchCriteria? criteria)
    {
        criteria = null;
        if (!TryParseOptional(request.Status, out ParkFitSourceReportStatus? status)
            || !TryParseOptional(request.Reason, out ParkFitSourceReportReason? reason))
        {
            return false;
        }

        criteria = new ParkFitSourceReportSearchCriteria(
            new PagedQuery(request.Page, request.Size),
            status,
            reason,
            request.ParkId);
        return true;
    }

    public static ParkFitSourceReportDto ToHttp(this ParkFitSourceReportResult result)
    {
        return new ParkFitSourceReportDto
        {
            ReportId = result.ReportId,
            ParkId = result.ParkId,
            ParkName = result.ParkName,
            EvidenceKind = result.EvidenceKind.ToString(),
            SourceUrl = result.SourceUrl,
            SourceReference = result.SourceReference,
            Reason = result.Reason.ToString(),
            Details = result.Details,
            Status = result.Status.ToString(),
            SubmittedAtUtc = result.SubmittedAtUtc,
            ReviewedAtUtc = result.ReviewedAtUtc,
            DecisionNote = result.DecisionNote,
            Revision = result.Revision,
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

        if (!Enum.TryParse(value, ignoreCase: false, out TEnum enumValue)
            || !Enum.IsDefined(enumValue))
        {
            return false;
        }

        parsed = enumValue;
        return true;
    }
}
