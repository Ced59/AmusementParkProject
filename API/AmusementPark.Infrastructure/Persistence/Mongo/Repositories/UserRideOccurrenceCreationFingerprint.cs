using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class UserRideOccurrenceCreationFingerprint
{
    public static string HashOperationKey(string clientOperationId)
    {
        return Hash(clientOperationId);
    }

    public static string HashPayload(RideOccurrenceCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<CreationItemPayload> items = request.Items
            .Select(item => new CreationItemPayload(
                request.VisitId.Value,
                request.UserId,
                item.ParkItemId,
                item.Moment.LocalTime,
                item.Moment.IsApproximate,
                item.Status,
                item.Source,
                item.PrivateNote,
                item.ConfirmHistoricalConflict))
            .ToArray();
        string canonicalPayload = JsonSerializer.Serialize(items);
        return Hash(canonicalPayload);
    }

    public static string HashReorderPayload(RideOccurrenceReorderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ReorderPayload payload = new ReorderPayload(
            request.VisitId.Value,
            request.UserId,
            request.OccurrenceId.Value,
            request.ExpectedVersion,
            request.AnchorOccurrenceId?.Value,
            request.Placement);
        return Hash(JsonSerializer.Serialize(payload));
    }

    private static string Hash(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private sealed record CreationItemPayload(
        string VisitId,
        string UserId,
        string ParkItemId,
        TimeOnly? LocalTime,
        bool IsApproximate,
        RideOccurrenceStatus Status,
        RideLogSource Source,
        string? PrivateNote,
        bool ConfirmHistoricalConflict);

    private sealed record ReorderPayload(
        string VisitId,
        string UserId,
        string OccurrenceId,
        long ExpectedVersion,
        string? AnchorOccurrenceId,
        RideOccurrencePlacement Placement);
}
