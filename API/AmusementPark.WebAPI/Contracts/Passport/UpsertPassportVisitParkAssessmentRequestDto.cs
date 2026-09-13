using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class UpsertPassportVisitParkAssessmentRequestDto
{
    public double Value { get; init; }

    public string? PrivateComment { get; init; }

    public long ExpectedVersion { get; init; }
}
