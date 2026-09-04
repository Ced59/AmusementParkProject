namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class DeletePassportVisitRequestDto
{
    public long ExpectedVersion { get; init; }

    public long ConfirmedOccurrenceCount { get; init; }

    public long ConfirmedAssessmentCount { get; init; }
}
