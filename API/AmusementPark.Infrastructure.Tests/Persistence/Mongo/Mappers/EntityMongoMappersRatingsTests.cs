using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class EntityMongoMappersRatingsTests
{
    [Fact]
    public void ToDomain_ShouldPreserveAggregateCalculationVersions()
    {
        RatingAggregateDocument document = new RatingAggregateDocument
        {
            Id = "aggregate-1",
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            MutationVersion = 4,
            CalculatedVersion = 3,
        };

        RatingAggregate result = document.ToDomain();

        Assert.Equal(4, result.MutationVersion);
        Assert.Equal(3, result.CalculatedVersion);
        Assert.False(result.IsCalculationCurrent);
    }

    [Fact]
    public void ToDocument_ShouldPreserveKnownAggregateCalculationVersions()
    {
        RatingAggregate aggregate = new RatingAggregate
        {
            Id = "aggregate-1",
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            MutationVersion = 5,
            CalculatedVersion = 5,
        };

        RatingAggregateDocument result = aggregate.ToDocument();

        Assert.Equal(5, result.MutationVersion);
        Assert.Equal(5, result.CalculatedVersion);
    }

    [Fact]
    public void ToDocument_WhenCalculationVersionsAreUnknown_ShouldRejectPersistence()
    {
        RatingAggregate aggregate = new RatingAggregate
        {
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => aggregate.ToDocument());

        Assert.Contains("version", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
