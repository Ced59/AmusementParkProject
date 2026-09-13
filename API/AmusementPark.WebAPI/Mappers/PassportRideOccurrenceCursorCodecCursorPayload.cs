using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using Microsoft.AspNetCore.WebUtilities;

namespace AmusementPark.WebAPI.Mappers;

internal sealed record PassportRideOccurrenceCursorCodecCursorPayload(
    int Version,
    long SortPosition,
    DateTime CreatedAtUtc,
    string OccurrenceId);
