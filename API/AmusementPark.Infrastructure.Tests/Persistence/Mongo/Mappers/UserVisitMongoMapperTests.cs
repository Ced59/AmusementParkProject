using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class UserVisitMongoMapperTests
{
    private static readonly DateTime CreatedAtUtc =
        new DateTime(2026, 8, 31, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Mapper_ShouldRoundTripACompletedVisitWithoutLosingPrivateState()
    {
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 8, 31),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            "Journée estivale",
            "Premier tour à l'ouverture.\nDernier tour après la parade.",
            CreatedAtUtc);
        visit.Complete(
            new DateOnly(2026, 8, 31),
            CreatedAtUtc.AddHours(12));

        UserVisitDocument document = visit.ToDocument();
        Visit restored = document.ToDomain();

        Assert.Equal(visit.Id, restored.Id);
        Assert.Equal(visit.UserId, restored.UserId);
        Assert.Equal(visit.ParkId, restored.ParkId);
        Assert.Equal(visit.Date, restored.Date);
        Assert.Equal(visit.TimeZoneId, restored.TimeZoneId);
        Assert.Equal(visit.ServiceDayConvention, restored.ServiceDayConvention);
        Assert.Equal(VisitStatus.Completed, restored.Status);
        Assert.Equal(VisitPrivacy.Private, restored.Privacy);
        Assert.Equal(visit.Title, restored.Title);
        Assert.Equal(visit.PrivateNote, restored.PrivateNote);
        Assert.Equal(2, restored.Version);
        Assert.Equal(visit.CreatedAtUtc, restored.CreatedAtUtc);
        Assert.Equal(visit.UpdatedAtUtc, restored.UpdatedAtUtc);
        Assert.Equal(visit.CompletedAtUtc, restored.CompletedAtUtc);
    }

    [Fact]
    public void Mapper_ShouldPreserveAnApproximatePartialDateWithoutInventingADay()
    {
        Visit visit = Visit.Create(
            VisitId.Parse("visit-2"),
            "user-1",
            "park-1",
            VisitDate.ForMonth(1998, 7, isApproximate: true),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            CreatedAtUtc);

        UserVisitDocument document = visit.ToDocument();
        Visit restored = document.ToDomain();

        Assert.Equal(1998, restored.Date.Year);
        Assert.Equal(7, restored.Date.Month);
        Assert.Null(restored.Date.Day);
        Assert.Equal(VisitDatePrecision.Month, restored.Date.Precision);
        Assert.True(restored.Date.IsApproximate);
        Assert.Null(restored.TimeZoneId);
        Assert.Null(restored.CompletedAtUtc);
    }

    [Fact]
    public void DocumentSerialization_ShouldUseStableCamelCaseFieldsAndStringEnums()
    {
        Visit visit = Visit.Create(
            VisitId.Parse("visit-3"),
            "user-1",
            "park-1",
            VisitDate.ForYear(2001),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            CreatedAtUtc);

        BsonDocument serialized = visit.ToDocument().ToBsonDocument();

        Assert.Equal("visit-3", serialized["_id"].AsString);
        Assert.Equal("user-1", serialized["userId"].AsString);
        Assert.Equal("park-1", serialized["parkId"].AsString);
        Assert.Equal("VisitStartLocalDate", serialized["serviceDayConvention"].AsString);
        Assert.Equal("Draft", serialized["status"].AsString);
        Assert.Equal("Private", serialized["privacy"].AsString);
        Assert.Equal(1, serialized["version"].AsInt64);
        BsonDocument date = serialized["date"].AsBsonDocument;
        Assert.Equal(2001, date["year"].AsInt32);
        Assert.Equal("Year", date["precision"].AsString);
        Assert.False(date.Contains("month"));
        Assert.False(date.Contains("day"));
        Assert.False(serialized.Contains("timeZoneId"));
        Assert.False(serialized.Contains("completedAtUtc"));
    }

    [Fact]
    public void ToDomain_ShouldRejectAnInvalidPersistedAggregate()
    {
        UserVisitDocument document = new UserVisitDocument
        {
            Id = "visit-invalid",
            UserId = "user-1",
            ParkId = "park-1",
            Date = new VisitDateDocument
            {
                Year = 2026,
                Precision = VisitDatePrecision.Year,
            },
            ServiceDayConvention = LocalServiceDayConvention.VisitStartLocalDate,
            Status = VisitStatus.Draft,
            Privacy = VisitPrivacy.Public,
            Version = 1,
            CreatedAt = CreatedAtUtc,
            UpdatedAt = CreatedAtUtc,
        };

        VisitValidationException exception = Assert.Throws<VisitValidationException>(
            () => document.ToDomain());

        Assert.Equal(VisitErrorCodes.InvalidPrivacy, exception.ErrorCode);
    }
}
