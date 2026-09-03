using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

internal static class PassportVisitHttpMappers
{
    public static CreateVisitCommand ToApplication(
        this CreatePassportVisitRequestDto request,
        string userId,
        string clientOperationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Date);

        return new CreateVisitCommand(
            userId,
            clientOperationId,
            request.ParkId,
            request.Date.Year,
            request.Date.Month,
            request.Date.Day,
            (VisitDatePrecision)request.Date.Precision,
            request.Date.IsApproximate,
            request.TimeZoneId,
            (LocalServiceDayConvention)request.ServiceDayConvention,
            request.Title,
            request.PrivateNote);
    }

    public static ListUserVisitsQuery ToApplication(
        this PassportVisitListRequestDto request,
        string userId,
        UserVisitListCursor? cursor)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ListUserVisitsQuery(
            userId,
            request.Limit,
            request.ParkId,
            request.Year,
            request.Status.HasValue
                ? (VisitStatus)request.Status.Value
                : null,
            cursor);
    }

    public static PassportVisitDto ToHttp(this VisitResult result)
    {
        return new PassportVisitDto
        {
            Id = result.Id,
            ParkId = result.ParkId,
            Date = new PassportVisitDateDto
            {
                Year = result.Date.Year,
                Month = result.Date.Month,
                Day = result.Date.Day,
                Precision = (PassportVisitDatePrecisionDto)result.Date.Precision,
                IsApproximate = result.Date.IsApproximate,
            },
            TimeZoneId = result.TimeZoneId,
            ServiceDayConvention =
                (PassportLocalServiceDayConventionDto)result.ServiceDayConvention,
            Status = (PassportVisitStatusDto)result.Status,
            Privacy = (PassportVisitPrivacyDto)result.Privacy,
            Title = result.Title,
            PrivateNote = result.PrivateNote,
            Version = result.Version,
            CreatedAtUtc = result.CreatedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
            CompletedAtUtc = result.CompletedAtUtc,
        };
    }

    public static PassportVisitPageDto ToHttp(this VisitPageResult result)
    {
        return new PassportVisitPageDto
        {
            Items = result.Items.Select(static visit => visit.ToHttp()).ToList(),
            NextCursor = result.NextCursor is null
                ? null
                : PassportVisitCursorCodec.Encode(result.NextCursor),
        };
    }
}
