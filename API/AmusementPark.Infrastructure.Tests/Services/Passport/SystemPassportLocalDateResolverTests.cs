using AmusementPark.Infrastructure.Services.Passport;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Passport;

public sealed class SystemPassportLocalDateResolverTests
{
    [Fact]
    public void Resolve_ShouldUseTheVisitTimeZoneAcrossTheUtcDayBoundary()
    {
        SystemPassportLocalDateResolver resolver = new SystemPassportLocalDateResolver();
        DateTime utcNow = new DateTime(2026, 9, 3, 23, 30, 0, DateTimeKind.Utc);

        DateOnly result = resolver.Resolve(utcNow, "Europe/Paris");

        Assert.Equal(new DateOnly(2026, 9, 4), result);
    }

    [Fact]
    public void Resolve_WithoutTimeZone_ShouldUseTheUtcDate()
    {
        SystemPassportLocalDateResolver resolver = new SystemPassportLocalDateResolver();
        DateTime utcNow = new DateTime(2026, 9, 3, 23, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(2026, 9, 3), resolver.Resolve(utcNow, null));
    }
}
