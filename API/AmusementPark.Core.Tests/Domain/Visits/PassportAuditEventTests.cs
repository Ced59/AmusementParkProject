using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class PassportAuditEventTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 4, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldBuildADeterministicIdentityAndHashTheCorrelationSeed()
    {
        PassportAuditEvent first = CreateEvent("client-secret-operation");
        PassportAuditEvent replay = CreateEvent("client-secret-operation");

        Assert.Equal("RideOccurrence:ride-1:2:RideOccurrenceChanged", first.Id);
        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(first.CorrelationId, replay.CorrelationId);
        Assert.Equal(64, first.CorrelationId.Length);
        Assert.DoesNotContain("client-secret-operation", first.CorrelationId);
    }

    [Fact]
    public void Create_WhenTimestampIsNotUtc_ShouldRejectTheEvent()
    {
        Assert.Throws<ArgumentException>(() => PassportAuditEvent.Create(
            "user-1",
            PassportAuditEntityType.Visit,
            "visit-1",
            "visit-1",
            "park-1",
            null,
            PassportAuditEventType.VisitCreated,
            1,
            null,
            new[] { PassportAuditChangedField.Visit },
            null,
            null,
            null,
            null,
            null,
            VisitStatus.Draft,
            null,
            null,
            null,
            null,
            false,
            "operation-1",
            PassportAuditOrigin.User,
            DateTime.SpecifyKind(NowUtc, DateTimeKind.Local)));
    }

    [Fact]
    public void Create_WhenChangedFieldsAreEmpty_ShouldRejectTheEvent()
    {
        Assert.Throws<ArgumentException>(() => PassportAuditEvent.Create(
            "user-1",
            PassportAuditEntityType.Visit,
            "visit-1",
            "visit-1",
            "park-1",
            null,
            PassportAuditEventType.VisitCreated,
            1,
            null,
            Array.Empty<PassportAuditChangedField>(),
            null,
            null,
            null,
            null,
            null,
            VisitStatus.Draft,
            null,
            null,
            null,
            null,
            false,
            "operation-1",
            PassportAuditOrigin.User,
            NowUtc));
    }

    private static PassportAuditEvent CreateEvent(string correlationSeed)
    {
        return PassportAuditEvent.Create(
            "user-1",
            PassportAuditEntityType.RideOccurrence,
            "ride-1",
            "visit-1",
            "park-1",
            "item-1",
            PassportAuditEventType.RideOccurrenceChanged,
            2,
            null,
            new[] { PassportAuditChangedField.Status },
            null,
            null,
            null,
            null,
            null,
            null,
            RideOccurrenceStatus.Attempted,
            RideOccurrenceStatus.Completed,
            1024,
            1024,
            false,
            correlationSeed,
            PassportAuditOrigin.User,
            NowUtc);
    }
}
