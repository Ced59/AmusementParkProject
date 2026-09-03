using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

internal static class PassportRideOccurrenceHttpMappers
{
    public static AddRideOccurrencesBatchCommand ToApplication(
        this CreatePassportRideOccurrencesBatchRequestDto request,
        string userId,
        string visitId,
        string clientOperationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new AddRideOccurrencesBatchCommand(
            userId,
            visitId,
            clientOperationId,
            request.Items.Select(ToApplication).ToArray());
    }

    public static AddRideOccurrencesBatchCommand ToApplication(
        this CreatePassportRideOccurrenceRequestDto request,
        string userId,
        string visitId,
        string clientOperationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new AddRideOccurrencesBatchCommand(
            userId,
            visitId,
            clientOperationId,
            new[] { ToApplication(request) });
    }

    public static UpdateRideOccurrenceCommand ToApplication(
        this UpdatePassportRideOccurrenceRequestDto request,
        string userId,
        string visitId,
        string occurrenceId)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new UpdateRideOccurrenceCommand(
            userId,
            visitId,
            occurrenceId,
            request.ExpectedVersion,
            request.Moment.LocalTime,
            request.Moment.IsApproximate,
            (RideOccurrenceStatus)request.Status,
            request.PrivateNote,
            request.ConfirmHistoricalConflict);
    }

    public static PassportRideOccurrenceDto ToHttp(this RideOccurrenceResult result)
    {
        return new PassportRideOccurrenceDto
        {
            Id = result.Id,
            VisitId = result.VisitId,
            ParkId = result.ParkId,
            ParkItemId = result.ParkItemId,
            SortPosition = result.SortPosition,
            Moment = new PassportRideOccurrenceMomentDto
            {
                LocalTime = result.Moment.LocalTime,
                IsApproximate = result.Moment.IsApproximate,
            },
            Status = (PassportRideOccurrenceStatusDto)result.Status,
            Source = (PassportRideLogSourceDto)result.Source,
            HistoricalConsistency =
                (PassportHistoricalConsistencyDto)result.HistoricalConsistency,
            PrivateNote = result.PrivateNote,
            CountsAsRide = result.CountsAsRide,
            Version = result.Version,
            CreatedAtUtc = result.CreatedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
            Assessment = result.Assessment is null
                ? null
                : new PassportRideAssessmentDto
                {
                    Value = result.Assessment.Value,
                    PrivateComment = result.Assessment.PrivateComment,
                    Revision = result.Assessment.Revision,
                    CreatedAtUtc = result.Assessment.CreatedAtUtc,
                    UpdatedAtUtc = result.Assessment.UpdatedAtUtc,
                },
            Target = result.Target is null
                ? null
                : new PassportRideOccurrenceTargetDto
                {
                    Name = result.Target.Name,
                    Category = result.Target.Category,
                    LifecycleStatus = result.Target.LifecycleStatus,
                    IsHistoricalSnapshot = result.Target.IsHistoricalSnapshot,
                },
        };
    }

    public static PassportRideOccurrencePageDto ToHttp(
        this RideOccurrencePageResult result)
    {
        return new PassportRideOccurrencePageDto
        {
            Items = result.Items.Select(static item => item.ToHttp()).ToArray(),
            NextCursor = result.NextCursor is null
                ? null
                : PassportRideOccurrenceCursorCodec.Encode(result.NextCursor),
        };
    }

    public static ListRideOccurrencesQuery ToApplication(
        this PassportRideOccurrenceListRequestDto request,
        string userId,
        string visitId,
        RideOccurrenceListCursor? cursor)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ListRideOccurrencesQuery(userId, visitId, request.Limit, cursor);
    }

    private static RideOccurrenceCreationItem ToApplication(
        CreatePassportRideOccurrenceRequestDto request)
    {
        return new RideOccurrenceCreationItem(
            request.ParkItemId,
            request.Moment.LocalTime,
            request.Moment.IsApproximate,
            (RideOccurrenceStatus)request.Status,
            request.PrivateNote,
            request.ConfirmHistoricalConflict,
            1);
    }

    private static RideOccurrenceCreationItem? ToApplication(
        CreatePassportRideOccurrenceBatchItemDto? request)
    {
        if (request is null)
        {
            return null;
        }

        return new RideOccurrenceCreationItem(
            request.ParkItemId,
            request.Moment.LocalTime,
            request.Moment.IsApproximate,
            (RideOccurrenceStatus)request.Status,
            request.PrivateNote,
            request.ConfirmHistoricalConflict,
            request.Count);
    }
}
