using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed record CreationItemPayload(
    string VisitId,
    string UserId,
    string ParkItemId,
    TimeOnly? LocalTime,
    bool IsApproximate,
    RideOccurrenceStatus Status,
    RideLogSource Source,
    string? PrivateNote,
    bool ConfirmHistoricalConflict);
