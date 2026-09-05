using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class SharePublicationRepositoryTests
{
    private const string TokenValue =
        "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA";

    private static readonly DateTime InitialUtc =
        new DateTime(2026, 9, 5, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_ShouldInsertTheMinimalDraftDocument()
    {
        Mock<IMongoCollection<SharePublicationDocument>> collection =
            new Mock<IMongoCollection<SharePublicationDocument>>(MockBehavior.Strict);
        SharePublicationDocument? inserted = null;
        collection.Setup(value => value.InsertOneAsync(
                It.IsAny<SharePublicationDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback((
                SharePublicationDocument document,
                InsertOneOptions _,
                CancellationToken _) => inserted = document)
            .Returns(Task.CompletedTask);
        SharePublicationRepository repository = new SharePublicationRepository(collection.Object);

        SharePublicationWriteOutcome outcome = await repository.CreateAsync(
            CreateDraft(),
            CancellationToken.None);

        Assert.Equal(SharePublicationWriteOutcome.Success, outcome);
        Assert.NotNull(inserted);
        Assert.Equal("publication-1", inserted.Id);
        Assert.Equal("user-1", inserted.OwnerUserId);
        Assert.Equal(SharePublicationStatus.Draft, inserted.Status);
        Assert.Equal(0, inserted.Version);
        Assert.Null(inserted.ShareToken);
        collection.VerifyAll();
    }

    [Fact]
    public async Task ReplaceAsync_WhenRevoking_ShouldRemoveTheTokenInTheVersionFencedReplacement()
    {
        Mock<IMongoCollection<SharePublicationDocument>> collection =
            new Mock<IMongoCollection<SharePublicationDocument>>(MockBehavior.Strict);
        FilterDefinition<SharePublicationDocument>? capturedFilter = null;
        SharePublicationDocument? replacement = null;
        collection.Setup(value => value.ReplaceOneAsync(
                It.IsAny<FilterDefinition<SharePublicationDocument>>(),
                It.IsAny<SharePublicationDocument>(),
                It.IsAny<ReplaceOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<SharePublicationDocument> filter,
                SharePublicationDocument document,
                ReplaceOptions options,
                CancellationToken _) =>
            {
                capturedFilter = filter;
                replacement = document;
                Assert.False(options.IsUpsert);
            })
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        SharePublicationRepository repository = new SharePublicationRepository(collection.Object);
        SharePublication publication = CreatePublished();
        publication.Revoke(1, InitialUtc.AddMinutes(2));

        SharePublicationWriteOutcome outcome = await repository.ReplaceAsync(
            publication,
            1,
            CancellationToken.None);

        Assert.Equal(SharePublicationWriteOutcome.Success, outcome);
        Assert.NotNull(capturedFilter);
        BsonDocument filter = Render(capturedFilter);
        Assert.Equal("publication-1", filter["_id"].AsString);
        Assert.Equal("user-1", filter["ownerUserId"].AsString);
        Assert.Equal(1, filter["version"].AsInt64);
        Assert.NotNull(replacement);
        Assert.Equal(SharePublicationStatus.Revoked, replacement.Status);
        Assert.Equal(ShareVisibility.Private, replacement.Visibility);
        Assert.Equal(2, replacement.Version);
        Assert.Null(replacement.ShareToken);
        Assert.False(replacement.ToBsonDocument().Contains("shareToken"));
        collection.VerifyAll();
    }

    [Fact]
    public async Task ReplaceAsync_WhenVersionNoLongerMatches_ShouldReportAConflict()
    {
        Mock<IMongoCollection<SharePublicationDocument>> collection =
            new Mock<IMongoCollection<SharePublicationDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.ReplaceOneAsync(
                It.IsAny<FilterDefinition<SharePublicationDocument>>(),
                It.IsAny<SharePublicationDocument>(),
                It.IsAny<ReplaceOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(0, 0, null));
        SharePublicationRepository repository = new SharePublicationRepository(collection.Object);
        SharePublication publication = CreatePublished();
        publication.RotateToken(
            ShareToken.Parse("ISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0-P0A"),
            1,
            InitialUtc.AddMinutes(2));

        SharePublicationWriteOutcome outcome = await repository.ReplaceAsync(
            publication,
            1,
            CancellationToken.None);

        Assert.Equal(SharePublicationWriteOutcome.Conflict, outcome);
        collection.VerifyAll();
    }

    [Theory]
    [InlineData("E11000 duplicate key index: idx_share_publication_token_unique dup key")]
    [InlineData("idx_share_publication_token_unique")]
    public void ClassifyDuplicateKey_WhenTokenIndexCollides_ShouldRequestTokenRegeneration(
        string message)
    {
        Assert.Equal(
            SharePublicationWriteOutcome.TokenCollision,
            SharePublicationRepository.ClassifyDuplicateKey(message));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("E11000 duplicate key index: _id_")]
    public void ClassifyDuplicateKey_WhenIdentityCollides_ShouldReportAConflict(string? message)
    {
        Assert.Equal(
            SharePublicationWriteOutcome.Conflict,
            SharePublicationRepository.ClassifyDuplicateKey(message));
    }

    [Fact]
    public void ClassifyDuplicateKey_WhenModernMongoReturnsAKeyPattern_ShouldDetectTheToken()
    {
        BsonDocument details = new BsonDocument(
            "keyPattern",
            new BsonDocument("shareToken", 1));

        Assert.Equal(
            SharePublicationWriteOutcome.TokenCollision,
            SharePublicationRepository.ClassifyDuplicateKey("duplicate key", details));
    }

    private static SharePublication CreateDraft()
    {
        return SharePublication.Create(
            SharePublicationId.Parse("publication-1"),
            "user-1",
            SharePublicationType.PassportProfile,
            "passport:user-1",
            ShareContentPolicy.CreatePrivateDefault(SharePublicationType.PassportProfile),
            3,
            InitialUtc);
    }

    private static SharePublication CreatePublished()
    {
        SharePublication publication = CreateDraft();
        publication.Publish(
            ShareToken.Parse(TokenValue),
            ShareVisibility.Unlisted,
            3,
            publication.ContentPolicy,
            0,
            InitialUtc.AddMinutes(1));
        return publication;
    }

    private static BsonDocument Render(FilterDefinition<SharePublicationDocument> filter)
    {
        IBsonSerializer<SharePublicationDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<SharePublicationDocument>();
        return filter.Render(
            new RenderArgs<SharePublicationDocument>(serializer, BsonSerializer.SerializerRegistry));
    }
}
