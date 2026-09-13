using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed record CreationPayload(
    string ParkId,
    int Year,
    int? Month,
    int? Day,
    VisitDatePrecision Precision,
    bool IsApproximate,
    string? TimeZoneId,
    LocalServiceDayConvention ServiceDayConvention,
    string? Title,
    string? PrivateNote);
