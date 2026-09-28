using AmusementPark.Application.Features.History.Services;
using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalEditorialInputMapperTests
{
    [Fact]
    public void ResolveSubject_ShouldRequireTheRoutedParkContextForHomonymousSubjects()
    {
        HistoricalSubject local = CreateSubject("park-1");
        HistoricalSubject external = CreateSubject("park-2");

        HistoricalSubject resolved = HistoricalEditorialInputMapper.ResolveSubject(
            new[] { local, external },
            HistoricalSubjectType.ParkItem,
            "shared-item",
            "park-1");
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalEditorialInputMapper.ResolveSubject(
                    new[] { local, external },
                    HistoricalSubjectType.ParkItem,
                    "shared-item",
                    "park-3"));

        Assert.Same(local, resolved);
        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidIdentifier, exception.ErrorCode);
    }

    private static HistoricalSubject CreateSubject(string contextParkId)
    {
        return new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            "shared-item",
            "Attraction homonyme",
            HistoricalSubjectPublicationPolicy.HistoricalOnly,
            contextParkId);
    }
}
