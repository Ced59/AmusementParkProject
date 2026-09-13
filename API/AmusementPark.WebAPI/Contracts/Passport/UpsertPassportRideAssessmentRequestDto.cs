using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class UpsertPassportRideAssessmentRequestDto
{
    public double Value { get; init; }

    public string? PrivateComment { get; init; }

    [Range(1, long.MaxValue)]
    public long ExpectedVersion { get; init; }
}
