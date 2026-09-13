using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class MutatePassportVisitStatusRequestDto
{
    public long ExpectedVersion { get; init; }
}
