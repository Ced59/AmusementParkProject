using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ImageRepositoryMutationPreconditionTests
{
    [Fact]
    public void BuildMutationPreconditionFilter_ShouldFenceTheObservedOwnershipScope()
    {
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildMutationPreconditionFilter(
                "avatar-1",
                new ImageMutationPrecondition(
                    ImageOwnerType.User,
                    "owner-before",
                    ImageCategory.Avatar,
                    true));
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments = new RenderArgs<ImageDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);

        BsonDocument rendered = filter.Render(arguments);

        Assert.Equal("avatar-1", rendered["_id"].AsString);
        Assert.Equal("User", rendered["ownerType"].AsString);
        Assert.Equal("owner-before", rendered["ownerId"].AsString);
        Assert.Equal("Avatar", rendered["category"].AsString);
        Assert.True(rendered["isCurrent"].AsBoolean);
    }
}
