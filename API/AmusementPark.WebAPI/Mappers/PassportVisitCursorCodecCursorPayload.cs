using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using Microsoft.AspNetCore.WebUtilities;

namespace AmusementPark.WebAPI.Mappers;

internal sealed record PassportVisitCursorCodecCursorPayload(
    int Version,
    int Year,
    int? Month,
    int? Day,
    VisitDatePrecision Precision,
    bool IsApproximate,
    DateTime UpdatedAtUtc,
    string VisitId);
