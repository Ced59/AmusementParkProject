using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphUpsertProcessorHistoryUpdateTests
{
    [Fact]
    public async Task ApplyAsync_WhenCanonicalNarrativeChanges_ShouldRetractCanonicalFactFirst()
    {
        Guid factId = Guid.NewGuid();
        HistoryEvent existing = BuildExistingEvent();
        existing.CanonicalFactId = factId;
        existing.CanonicalizationState = HistoricalNarrativeCanonicalizationState.Canonicalized;
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(existing);
        context.HistoricalFactRepository
            .Setup(repository => repository.GetLatestRevisionAsync(factId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCanonicalFact(factId, HistoricalSubjectType.Park, "park-1"));
        context.HistoricalFactRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id == factId
                    && fact.State == HistoricalFactState.Retracted
                    && fact.PublicationState == HistoricalPublicationState.Withdrawn),
                It.Is<HistoricalReviewEvent>(review => review.EventType == HistoricalReviewEventType.Retracted),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        string document = BuildDocument("""
        "sources": [
          {
            "label": "Nouvelle archive",
            "url": "https://example.test/new-history"
          }
        ]
        """);

        ApplicationResult<ParkGraphUpsertResult> result = await context.ApplyAsync(document);

        Assert.True(result.IsSuccess);
        context.HistoricalFactRepository.VerifyAll();
        context.HistoryEventRepository.Verify(
            repository => repository.UpdateAsync(
                "history-1",
                It.Is<HistoryEvent>(historyEvent => historyEvent.CanonicalFactId == factId),
                It.IsAny<DateTime>(),
                factId,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_WhenCanonicalNarrativeUpdateFails_ShouldRestoreCanonicalFact()
    {
        Guid factId = Guid.NewGuid();
        HistoryEvent existing = BuildExistingEvent();
        existing.CanonicalFactId = factId;
        existing.CanonicalizationState = HistoricalNarrativeCanonicalizationState.Canonicalized;
        HistoricalFact canonicalFact = CreateCanonicalFact(
            factId,
            HistoricalSubjectType.Park,
            "park-1");
        HistoricalFact retraction = canonicalFact.CreateRetraction(
            canonicalFact.RecordedAtUtc.AddSeconds(1));
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(existing);
        context.HistoricalFactRepository
            .SetupSequence(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(canonicalFact)
            .ReturnsAsync(retraction);
        context.HistoricalFactRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id == factId
                    && fact.PublicationState == HistoricalPublicationState.Withdrawn),
                It.Is<HistoricalReviewEvent>(review => review.EventType
                    == HistoricalReviewEventType.Retracted),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        context.HistoricalFactRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id == factId
                    && fact.Revision == retraction.Revision + 1
                    && fact.PublicationState == HistoricalPublicationState.Published),
                It.Is<HistoricalReviewEvent>(review => review.EventType
                    == HistoricalReviewEventType.Published),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        context.FailNextHistoryUpdate(new TimeoutException("write failed before commit"));
        string document = BuildDocument("""
        "sources": [
          {
            "label": "Nouvelle archive",
            "url": "https://example.test/new-history"
          }
        ]
        """);

        await Assert.ThrowsAsync<TimeoutException>(() => context.ApplyAsync(document));

        context.HistoricalFactRepository.VerifyAll();
        Assert.Equal(factId, context.ReadPersistedEvent().CanonicalFactId);
    }

    [Fact]
    public async Task ApplyAsync_WhenCanonicalizationFailsAfterUpdate_ShouldRestoreCanonicalFact()
    {
        Guid factId = Guid.NewGuid();
        HistoryEvent existing = BuildExistingEvent();
        existing.CanonicalFactId = factId;
        existing.CanonicalizationState = HistoricalNarrativeCanonicalizationState.Canonicalized;
        HistoricalFact canonicalFact = CreateCanonicalFact(
            factId,
            HistoricalSubjectType.Park,
            "park-1");
        HistoricalFact retraction = canonicalFact.CreateRetraction(
            canonicalFact.RecordedAtUtc.AddSeconds(1));
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(existing);
        context.HistoricalFactRepository
            .SetupSequence(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(canonicalFact)
            .ReturnsAsync(retraction);
        context.HistoricalFactRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                It.Is<Guid>(candidateId => candidateId != factId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoricalFact?)null);
        context.HistoricalFactRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id == factId
                    && fact.PublicationState == HistoricalPublicationState.Withdrawn),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        context.HistoricalFactRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id == factId
                    && fact.Revision == retraction.Revision + 1
                    && fact.PublicationState == HistoricalPublicationState.Published),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        context.FailCanonicalization(new TimeoutException("canonicalization failed"));
        string document = BuildDocument("""
        "sources": [
          {
            "label": "Nouvelle archive",
            "url": "https://example.test/new-history"
          }
        ]
        """);

        await Assert.ThrowsAsync<TimeoutException>(() => context.ApplyAsync(document));

        context.HistoricalFactRepository.VerifyAll();
        Assert.Equal(factId, context.ReadPersistedEvent().CanonicalFactId);
    }

    [Fact]
    public async Task ApplyAsync_WhenCanonicalNarrativeUpdateCommitsWithoutAcknowledgement_ShouldCanonicalizeCommittedUpdate()
    {
        Guid factId = Guid.NewGuid();
        HistoryEvent existing = BuildExistingEvent();
        existing.CanonicalFactId = factId;
        existing.CanonicalizationState = HistoricalNarrativeCanonicalizationState.Canonicalized;
        HistoricalFact canonicalFact = CreateCanonicalFact(
            factId,
            HistoricalSubjectType.Park,
            "park-1");
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(existing);
        context.HistoricalFactRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(canonicalFact);
        context.HistoricalFactRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id == factId
                    && fact.PublicationState == HistoricalPublicationState.Withdrawn),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        HistoryEvent committedUpdate = BuildExistingEvent();
        committedUpdate.Sources = new List<HistorySourceReference>
        {
            new HistorySourceReference
            {
                Label = "Nouvelle archive",
                Url = "https://example.test/new-history",
            },
        };
        committedUpdate.CanonicalFactId = factId;
        committedUpdate.CanonicalizationState = HistoricalNarrativeCanonicalizationState.PendingReview;
        committedUpdate.UpdatedAtUtc = existing.UpdatedAtUtc.AddSeconds(1);
        Guid committedMutationId = Guid.Empty;
        context.HistoryEventRepository
            .Setup(repository => repository.UpdateAsync(
                "history-1",
                It.IsAny<HistoryEvent>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, HistoryEvent _, DateTime _, Guid? _, Guid mutationId, CancellationToken _) =>
            {
                committedMutationId = mutationId;
                return Task.FromException<HistoryEvent?>(
                    new TimeoutException("acknowledgement lost after commit"));
            });
        context.HistoryEventRepository
            .SetupSequence(repository => repository.GetCommittedUpdateAsync(
                "history-1",
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("first mutation lookup failed"))
            .ThrowsAsync(new TimeoutException("second mutation lookup failed"));
        context.HistoryEventRepository
            .Setup(repository => repository.GetMutationSnapshotAsync(
                "history-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new HistoryEventMutationSnapshot(
                committedUpdate,
                committedMutationId));
        string document = BuildDocument("""
        "sources": [
          {
            "label": "Nouvelle archive",
            "url": "https://example.test/new-history"
          }
        ]
        """);

        ApplicationResult<ParkGraphUpsertResult> result = await context.ApplyAsync(document);

        Assert.True(result.IsSuccess);
        context.HistoricalNarrativeCanonicalizer.Verify(
            canonicalizer => canonicalizer.CanonicalizeAsync(
                It.Is<HistoryEvent>(historyEvent => historyEvent.Sources.Any(
                    source => source.Url == "https://example.test/new-history")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        context.HistoricalFactRepository.VerifyAll();
    }

    [Fact]
    public async Task PreviewAndApplyAsync_WhenExistingArticleTextChanges_ShouldReportAndPersistUpdate()
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(BuildExistingEvent());
        string document = BuildDocument($$"""
        "article": {{BuildArticleJson(introText: "Looping Star arrive en 1979, puis Wild Water Slide en 1980.")}}
        """);

        ApplicationResult<ParkGraphUpsertResult> preview = await context.PreviewAsync(document);

        Assert.True(preview.IsSuccess);
        ParkGraphUpsertChange previewChange = AssertHistoryChange(preview, "Updated", "article");
        Assert.Equal(1, preview.Value!.Counts.Updated);
        Assert.NotEqual(
            previewChange.Fields.Single(static field => field.Field == "article").OldValue,
            previewChange.Fields.Single(static field => field.Field == "article").NewValue);
        context.HistoryEventRepository.Verify(
            value => value.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<HistoryEvent>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(document);

        Assert.True(apply.IsSuccess);
        AssertHistoryChange(apply, "Updated", "article");
        context.HistoryEventRepository.Verify(
            value => value.UpdateAsync(
                "history-1",
                It.IsAny<HistoryEvent>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        HistoryEvent persistedEvent = context.ReadPersistedEvent();
        HistoryArticleBlock introBlock = Assert.Single(persistedEvent.Article!.Blocks, static block => block.Id == "intro");
        Assert.Contains(
            introBlock.Texts,
            static text => text.LanguageCode == "fr" && text.Value == "Looping Star arrive en 1979, puis Wild Water Slide en 1980.");
    }

    [Fact]
    public async Task ApplyAsync_WhenArticleSourceIsAdded_ShouldReportAndPersistUpdate()
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(BuildExistingEvent());
        string sources = """
        [
          {
            "label": "Archives Mirapolis",
            "url": "https://example.test/history",
            "accessedAt": "2026-08-04"
          },
          {
            "label": "Catalogue 1980",
            "url": "https://example.test/catalogue-1980",
            "accessedAt": "2026-08-04"
          }
        ]
        """;
        string document = BuildDocument($$"""
        "article": {{BuildArticleJson(sourcesJson: sources)}}
        """);

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(document);

        Assert.True(apply.IsSuccess);
        AssertHistoryChange(apply, "Updated", "article");
        HistoryArticle persistedArticle = Assert.IsType<HistoryArticle>(context.ReadPersistedEvent().Article);
        Assert.Equal(2, persistedArticle.Sources.Count);
        Assert.Contains(
            persistedArticle.Sources,
            static source => source.Url == "https://example.test/catalogue-1980");
    }

    [Fact]
    public async Task ApplyAsync_WhenArticleBlockImageAndCaptionChange_ShouldReportAndPersistUpdate()
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(BuildExistingEvent());
        string document = BuildDocument($$"""
        "article": {{BuildArticleJson(blockImageId: "image-block-2", blockCaption: "Wild Water Slide en construction.")}}
        """);

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(document);

        Assert.True(apply.IsSuccess);
        AssertHistoryChange(apply, "Updated", "article");
        HistoryArticleBlock imageBlock = Assert.Single(
            context.ReadPersistedEvent().Article!.Blocks,
            static block => block.Id == "photo");
        Assert.Equal("image-block-2", imageBlock.ImageId);
        Assert.Contains(
            imageBlock.Captions,
            static caption => caption.LanguageCode == "fr" && caption.Value == "Wild Water Slide en construction.");
    }

    [Fact]
    public async Task ApplyAsync_WhenArticleIsStructurallyUnchanged_ShouldNotUpdateHistoryEvent()
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(BuildExistingEvent());
        string document = BuildDocument($$"""
        "article": {{BuildArticleJson(introText: "  Looping Star et Wild Water Slide arrivent en 1979.  ", includeBlockIds: false)}}
        """);

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(document);

        Assert.True(apply.IsSuccess);
        ParkGraphUpsertChange change = AssertHistoryChange(apply, "Unchanged");
        Assert.Empty(change.Fields);
        context.HistoryEventRepository.Verify(
            value => value.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<HistoryEvent>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(new[] { "photo", "intro" }, context.ReadPersistedEvent().Article!.Blocks.Select(static block => block.Id));
    }

    [Fact]
    public async Task ApplyAsync_WhenArticleIsUnchangedButCanonicalFactIsMissing_ShouldRebuildCanonicalResources()
    {
        HistoryEvent existing = BuildExistingEvent();
        existing.CanonicalFactId = Guid.NewGuid();
        existing.CanonicalizationState = HistoricalNarrativeCanonicalizationState.Canonicalized;
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(existing);
        context.SetCanonicalRepairRequired(true);
        string document = BuildDocument($$"""
        "article": {{BuildArticleJson(introText: "  Looping Star et Wild Water Slide arrivent en 1979.  ", includeBlockIds: false)}}
        """);

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(document);

        Assert.True(apply.IsSuccess);
        AssertHistoryChange(apply, "Updated", "canonicalHistory");
        context.HistoryEventRepository.Verify(
            value => value.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<HistoryEvent>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        context.HistoricalNarrativeCanonicalizer.Verify(
            value => value.CanonicalizeAsync(
                It.Is<HistoryEvent>(historyEvent => historyEvent.Id == "history-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        context.HistoricalFactRepository.Verify(
            repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id == existing.CanonicalFactId
                    && fact.PublicationState == HistoricalPublicationState.Withdrawn),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_WhenArticleImageKeysCannotBeResolved_ShouldPreserveExistingImageIds()
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(BuildExistingEvent());
        string document = BuildDocument($$"""
        "article": {{BuildArticleJsonWithUnresolvedImageKeys()}}
        """);

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(document);

        Assert.True(apply.IsSuccess);
        AssertHistoryChange(apply, "Unchanged");
        Assert.Contains(apply.Value!.Warnings, static warning => warning.Contains("missing-main", StringComparison.Ordinal));
        Assert.Contains(apply.Value.Warnings, static warning => warning.Contains("missing-block", StringComparison.Ordinal));
        Assert.Contains(apply.Value.Warnings, static warning => warning.Contains("missing-gallery", StringComparison.Ordinal));
        context.HistoryEventRepository.Verify(
            value => value.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<HistoryEvent>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        HistoryArticle persistedArticle = Assert.IsType<HistoryArticle>(context.ReadPersistedEvent().Article);
        Assert.Equal("image-main-1", persistedArticle.MainImageId);
        HistoryArticleBlock imageBlock = Assert.Single(persistedArticle.Blocks, static block => block.Id == "photo");
        Assert.Equal("image-block-1", imageBlock.ImageId);
        Assert.Equal(new[] { "image-gallery-1", "image-gallery-2" }, imageBlock.ImageIds);
    }

    [Fact]
    public async Task PreviewAsync_WhenArticleImageKeysCannotBeResolved_ShouldReportArticleUpdate()
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(BuildExistingEvent());
        string document = BuildDocument($$"""
        "article": {{BuildArticleJsonWithUnresolvedImageKeys()}}
        """);

        ApplicationResult<ParkGraphUpsertResult> preview = await context.PreviewAsync(document);

        Assert.True(preview.IsSuccess);
        AssertHistoryChange(preview, "Updated", "article");
        context.HistoryEventRepository.Verify(
            value => value.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<HistoryEvent>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_WhenArticleImageIdsAreExplicitlyCleared_ShouldRemoveExistingImageIds()
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(BuildExistingEvent());
        string articleJson = BuildArticleJson()
            .Replace("\"mainImageId\": \"image-main-1\"", "\"mainImageId\": null", StringComparison.Ordinal)
            .Replace("\"imageId\": \"image-block-1\"", "\"imageId\": null", StringComparison.Ordinal)
            .Replace("\"imageIds\": [\"image-gallery-1\", \"image-gallery-2\"]", "\"imageIds\": []", StringComparison.Ordinal);
        string document = BuildDocument($$"""
        "article": {{articleJson}}
        """);

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(document);

        Assert.True(apply.IsSuccess);
        AssertHistoryChange(apply, "Updated", "article");
        HistoryArticle persistedArticle = Assert.IsType<HistoryArticle>(context.ReadPersistedEvent().Article);
        Assert.Null(persistedArticle.MainImageId);
        HistoryArticleBlock imageBlock = Assert.Single(persistedArticle.Blocks, static block => block.Id == "photo");
        Assert.Null(imageBlock.ImageId);
        Assert.Empty(imageBlock.ImageIds);
    }

    [Fact]
    public async Task ApplyAsync_WhenArticleIsExplicitlyNull_ShouldRemoveAndPersistArticle()
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(BuildExistingEvent());
        string document = BuildDocument("\"article\": null");

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(document);

        Assert.True(apply.IsSuccess);
        ParkGraphUpsertChange change = AssertHistoryChange(apply, "Updated", "article");
        ParkGraphUpsertFieldChange articleChange = Assert.Single(change.Fields, static field => field.Field == "article");
        Assert.NotNull(articleChange.OldValue);
        Assert.Null(articleChange.NewValue);
        Assert.Null(context.ReadPersistedEvent().Article);
    }

    [Fact]
    public async Task ApplyAsync_WhenEventSourcesHaveSameCountButDifferentContent_ShouldReportAndPersistUpdate()
    {
        HistoryUpsertTestContext context = new HistoryUpsertTestContext(BuildExistingEvent());
        string document = BuildDocument("""
        "sources": [
          {
            "label": "Nouvelle archive",
            "url": "https://example.test/new-history",
            "accessedAt": "2026-08-04"
          }
        ]
        """);

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(document);

        Assert.True(apply.IsSuccess);
        AssertHistoryChange(apply, "Updated", "sources");
        HistorySourceReference source = Assert.Single(context.ReadPersistedEvent().Sources);
        Assert.Equal("Nouvelle archive", source.Label);
        Assert.Equal("https://example.test/new-history", source.Url);
    }

    private static ParkGraphUpsertChange AssertHistoryChange(
        ApplicationResult<ParkGraphUpsertResult> result,
        string expectedChangeType,
        string? expectedField = null)
    {
        ParkGraphUpsertChange change = Assert.Single(
            result.Value!.Changes,
            static candidate => candidate.EntityType == "HistoryEvent");
        Assert.Equal(expectedChangeType, change.ChangeType);
        if (expectedField is not null)
        {
            Assert.Contains(change.Fields, field => field.Field == expectedField);
        }

        return change;
    }

    private static string BuildDocument(string historyPatch)
    {
        return $$"""
        {
          "mode": "merge",
          "historyEvents": [
            {
              "owner": "park",
              "ownerId": "park-1",
              "key": "history-1979",
              "eventType": "Opening",
              "date": "1979",
              {{historyPatch}}
            }
          ]
        }
        """;
    }

    private static string BuildArticleJson(
        string introText = "Looping Star et Wild Water Slide arrivent en 1979.",
        string? sourcesJson = null,
        string blockImageId = "image-block-1",
        string blockCaption = "Looping Star en 1979.",
        bool includeBlockIds = true)
    {
        sourcesJson ??= """
        [
          {
            "label": "Archives Mirapolis",
            "url": "https://example.test/history",
            "accessedAt": "2026-08-04"
          }
        ]
        """;
        string introId = includeBlockIds ? "\"id\": \"intro\"," : string.Empty;
        string photoId = includeBlockIds ? "\"id\": \"photo\"," : string.Empty;

        return $$"""
        {
          "slug": "arrivees-1979",
          "titles": {
            "en": "New attractions in 1979",
            "fr": "Nouvelles attractions en 1979"
          },
          "subtitles": {
            "fr": "Deux nouveautés majeures"
          },
          "summaries": {
            "fr": "Retour sur les nouveautés annoncées."
          },
          "mainImageId": "image-main-1",
          "blocks": [
            {
              {{introId}}
              "type": "Paragraph",
              "sortOrder": 1,
              "texts": {
                "en": "Looping Star and Wild Water Slide arrive in 1979.",
                "fr": "{{introText}}"
              }
            },
            {
              {{photoId}}
              "type": "Image",
              "sortOrder": 2,
              "imageId": "{{blockImageId}}",
              "imageIds": ["image-gallery-1", "image-gallery-2"],
              "captions": {
                "fr": "{{blockCaption}}"
              }
            }
          ],
          "sources": {{sourcesJson}},
          "isPublished": true
        }
        """;
    }

    private static string BuildArticleJsonWithUnresolvedImageKeys()
    {
        return BuildArticleJson()
            .Replace("\"mainImageId\": \"image-main-1\"", "\"mainImageKey\": \"missing-main\"", StringComparison.Ordinal)
            .Replace("\"imageId\": \"image-block-1\"", "\"imageKey\": \"missing-block\"", StringComparison.Ordinal)
            .Replace("\"imageIds\": [\"image-gallery-1\", \"image-gallery-2\"]", "\"imageKeys\": [\"missing-gallery\"]", StringComparison.Ordinal);
    }

    private static HistoryEvent BuildExistingEvent()
    {
        return new HistoryEvent
        {
            Id = "history-1",
            Key = "history-1979",
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            ParkId = "park-1",
            Year = 1979,
            DatePrecision = HistoryDatePrecision.Year,
            EventType = ParkHistoryEventType.Opening.ToString(),
            IsMajor = true,
            IsVisible = true,
            Sources = new List<HistorySourceReference>
            {
                new HistorySourceReference
                {
                    Label = "Archive existante",
                    Url = "https://example.test/old-history",
                    AccessedAt = "2026-08-04",
                },
            },
            Article = new HistoryArticle
            {
                Slug = "arrivees-1979",
                Titles = new List<LocalizedText>
                {
                    new LocalizedText("fr", "Nouvelles attractions en 1979"),
                    new LocalizedText("en", "New attractions in 1979"),
                },
                Subtitles = new List<LocalizedText>
                {
                    new LocalizedText("fr", "Deux nouveautés majeures"),
                },
                Summaries = new List<LocalizedText>
                {
                    new LocalizedText("fr", "Retour sur les nouveautés annoncées."),
                },
                MainImageId = "image-main-1",
                Blocks = new List<HistoryArticleBlock>
                {
                    new HistoryArticleBlock
                    {
                        Id = "photo",
                        Type = HistoryArticleBlockType.Image,
                        SortOrder = 2,
                        ImageId = "image-block-1",
                        ImageIds = new List<string> { "image-gallery-1", "image-gallery-2" },
                        Captions = new List<LocalizedText>
                        {
                            new LocalizedText("fr", "Looping Star en 1979."),
                        },
                    },
                    new HistoryArticleBlock
                    {
                        Id = "intro",
                        Type = HistoryArticleBlockType.Paragraph,
                        SortOrder = 1,
                        Texts = new List<LocalizedText>
                        {
                            new LocalizedText("fr", "Looping Star et Wild Water Slide arrivent en 1979."),
                            new LocalizedText("en", "Looping Star and Wild Water Slide arrive in 1979."),
                        },
                    },
                },
                Sources = new List<HistorySourceReference>
                {
                    new HistorySourceReference
                    {
                        Label = "Archives Mirapolis",
                        Url = "https://example.test/history",
                        AccessedAt = "2026-08-04",
                    },
                },
                IsPublished = true,
            },
        };
    }

    private static HistoricalFact CreateCanonicalFact(
        Guid factId,
        HistoricalSubjectType subjectType,
        string subjectId)
    {
        DateTime recordedAtUtc = DateTime.UtcNow.AddMinutes(-1);
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(1979));
        return new HistoricalFact(
            factId,
            new HistoricalSubject(
                subjectType,
                subjectId,
                "Cible historique",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            HistoricalFactType.Opening,
            period,
            HistoricalFactState.Verified,
            HistoricalImportance.Standard,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            new[]
            {
                new HistoricalSourceRevisionReference(
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    1,
                    subjectType,
                    subjectId,
                    HistoricalFactType.Opening,
                    period,
                    HistoricalEvidencePosition.Supports,
                    new[]
                    {
                        HistoricalSourceScope.SubjectIdentity,
                        HistoricalSourceScope.FactType,
                        HistoricalSourceScope.Period,
                        HistoricalSourceScope.HistoricalLabel,
                    },
                    "Cible historique",
                    null,
                    null,
                    null,
                    null,
                    LifecycleBoundaryMeaning.FirstOperatingDay,
                    null,
                    null),
            },
            null,
            null,
            "history-1",
            recordedAtUtc.AddMinutes(-2),
            recordedAtUtc.AddMinutes(-1),
            "hist-v1",
            4,
            3,
            recordedAtUtc);
    }

}
