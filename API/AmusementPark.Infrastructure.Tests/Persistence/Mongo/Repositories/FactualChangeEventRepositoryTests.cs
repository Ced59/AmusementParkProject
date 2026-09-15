using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class FactualChangeEventRepositoryTests
{
    [Fact]
    public void BuildSearchFilter_ShouldCombineEveryBoundedAdministrationFilter()
    {
        FactualChangeEventSearchCriteria criteria = new FactualChangeEventSearchCriteria(
            new PagedQuery(),
            FactualChangeStatus.Draft,
            FactualTargetType.Park,
            FactualEventType.OpeningCalendarChanged,
            DataConfidence.High);

        BsonDocument rendered = Render(
            FactualChangeEventRepository.BuildSearchFilter(criteria));
        string json = rendered.ToJson();

        Assert.Contains("status", json, StringComparison.Ordinal);
        Assert.Contains("target.type", json, StringComparison.Ordinal);
        Assert.Contains("type", json, StringComparison.Ordinal);
        Assert.Contains("confidence", json, StringComparison.Ordinal);
    }

    private static BsonDocument Render(
        FilterDefinition<FactualChangeEventDocument> filter)
    {
        IBsonSerializer<FactualChangeEventDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<FactualChangeEventDocument>();
        RenderArgs<FactualChangeEventDocument> arguments =
            new RenderArgs<FactualChangeEventDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
