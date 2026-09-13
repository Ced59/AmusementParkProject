using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportVisitDateDto
{
    public int Year { get; init; }

    public int? Month { get; init; }

    public int? Day { get; init; }

    public PassportVisitDatePrecisionDto Precision { get; init; }

    public bool IsApproximate { get; init; }
}
