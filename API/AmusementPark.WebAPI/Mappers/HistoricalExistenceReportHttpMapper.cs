using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.HistoricalExistenceReports.Commands;
using AmusementPark.Application.Features.HistoricalExistenceReports.Models;
using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

public static class HistoricalExistenceReportHttpMapper
{
    public static SubmitHistoricalExistenceReportCommand ToCommand(
        this SubmitHistoricalExistenceReportRequestDto request,
        string userId,
        string visitId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SubmitHistoricalExistenceReportCommand(
            userId,
            visitId,
            request.ClaimedName,
            request.SourceUrl,
            request.SourceReference,
            request.Details);
    }

    public static bool TryToCommand(
        this ReviewHistoricalExistenceReportRequestDto request,
        string reportId,
        string reviewerUserId,
        out ReviewHistoricalExistenceReportCommand? command)
    {
        command = null;
        if (!Enum.TryParse(
                request.Decision,
                ignoreCase: false,
                out HistoricalExistenceReportStatus decision)
            || !Enum.IsDefined(decision))
        {
            return false;
        }

        command = new ReviewHistoricalExistenceReportCommand(
            reportId,
            decision,
            reviewerUserId,
            request.DecisionNote,
            request.ExpectedRevision);
        return true;
    }

    public static bool TryToCriteria(
        this HistoricalExistenceReportSearchRequestDto request,
        out HistoricalExistenceReportSearchCriteria? criteria)
    {
        criteria = null;
        HistoricalExistenceReportStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse(
                    request.Status,
                    ignoreCase: false,
                    out HistoricalExistenceReportStatus parsed)
                || !Enum.IsDefined(parsed))
            {
                return false;
            }

            status = parsed;
        }

        criteria = new HistoricalExistenceReportSearchCriteria(
            new PagedQuery(request.Page, request.Size),
            status,
            request.ParkId);
        return true;
    }

    public static HistoricalExistenceReportDto ToHttp(
        this HistoricalExistenceReportResult result)
    {
        return new HistoricalExistenceReportDto
        {
            ReportId = result.ReportId,
            ParkId = result.ParkId,
            ParkName = result.ParkName,
            VisitDate = new PassportVisitDateDto
            {
                Year = result.VisitDate.Year,
                Month = result.VisitDate.Month,
                Day = result.VisitDate.Day,
                Precision = (PassportVisitDatePrecisionDto)result.VisitDate.Precision,
                IsApproximate = result.VisitDate.IsApproximate,
            },
            ClaimedName = result.ClaimedName,
            SourceUrl = result.SourceUrl,
            SourceReference = result.SourceReference,
            Details = result.Details,
            Status = result.Status.ToString(),
            SubmittedAtUtc = result.SubmittedAtUtc,
            ReviewedAtUtc = result.ReviewedAtUtc,
            DecisionNote = result.DecisionNote,
            Revision = result.Revision,
        };
    }
}
