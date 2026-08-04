using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
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
            value => value.UpdateAsync(It.IsAny<string>(), It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        ApplicationResult<ParkGraphUpsertResult> apply = await context.ApplyAsync(document);

        Assert.True(apply.IsSuccess);
        AssertHistoryChange(apply, "Updated", "article");
        context.HistoryEventRepository.Verify(
            value => value.UpdateAsync("history-1", It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()),
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
            value => value.UpdateAsync(It.IsAny<string>(), It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(new[] { "photo", "intro" }, context.ReadPersistedEvent().Article!.Blocks.Select(static block => block.Id));
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

    private static HistoryEvent CloneHistoryEvent(HistoryEvent source)
    {
        string json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<HistoryEvent>(json)
            ?? throw new InvalidOperationException("The history event test fixture could not be cloned.");
    }

    private sealed class HistoryUpsertTestContext
    {
        private readonly Mock<IParkRepository> parkRepository;
        private readonly Mock<IParkGraphUpsertHistoryRepository> upsertHistoryRepository;
        private readonly Mock<ISearchProjectionWriter> searchProjectionWriter;
        private readonly Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier;
        private HistoryEvent persistedEvent;

        public HistoryUpsertTestContext(HistoryEvent existingEvent)
        {
            this.persistedEvent = CloneHistoryEvent(existingEvent);
            this.parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
            this.parkRepository
                .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(static () => BuildPark());
            this.parkRepository
                .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, Park park, CancellationToken _) => park);

            this.HistoryEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
            this.HistoryEventRepository
                .Setup(value => value.GetByOwnerKeyAsync(
                    HistoryEntityType.Park,
                    "park-1",
                    "history-1979",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CloneHistoryEvent(this.persistedEvent));
            this.HistoryEventRepository
                .Setup(value => value.UpdateAsync("history-1", It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()))
                .Callback<string, HistoryEvent, CancellationToken>((_, historyEvent, _) =>
                {
                    this.persistedEvent = CloneHistoryEvent(historyEvent);
                })
                .ReturnsAsync((string _, HistoryEvent historyEvent, CancellationToken _) => CloneHistoryEvent(historyEvent));

            this.upsertHistoryRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
            this.upsertHistoryRepository
                .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
            this.searchProjectionWriter
                .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
            this.publicSeoUpdateNotifier
                .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.Processor = new ParkGraphUpsertProcessor(
                this.parkRepository.Object,
                Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
                Mock.Of<IParkItemRepository>(MockBehavior.Strict),
                Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
                Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
                Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
                Mock.Of<IImageRepository>(MockBehavior.Strict),
                Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
                this.searchProjectionWriter.Object,
                this.upsertHistoryRepository.Object,
                this.publicSeoUpdateNotifier.Object,
                MeasurementConversionService.Instance,
                historyEventRepository: this.HistoryEventRepository.Object);
        }

        public Mock<IHistoryEventRepository> HistoryEventRepository { get; }

        private ParkGraphUpsertProcessor Processor { get; }

        public async Task<ApplicationResult<ParkGraphUpsertResult>> PreviewAsync(string rawJson)
        {
            return await this.ProcessAsync(rawJson, false);
        }

        public async Task<ApplicationResult<ParkGraphUpsertResult>> ApplyAsync(string rawJson)
        {
            return await this.ProcessAsync(rawJson, true);
        }

        public HistoryEvent ReadPersistedEvent()
        {
            return CloneHistoryEvent(this.persistedEvent);
        }

        private static Park BuildPark()
        {
            return new Park
            {
                Id = "park-1",
                Name = "Mirapolis",
                CountryCode = "FR",
                IsVisible = true,
                AdminReviewStatus = AdminReviewStatus.Validated,
            };
        }

        private async Task<ApplicationResult<ParkGraphUpsertResult>> ProcessAsync(string rawJson, bool apply)
        {
            using JsonDocument document = JsonDocument.Parse(rawJson);
            ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
            {
                TargetParkId = "park-1",
                CreateIfMissing = false,
                ReplaceCollections = false,
                Document = document.RootElement.Clone(),
                RawJson = rawJson,
            };

            return apply
                ? await this.Processor.ApplyAsync(request, "user-1", CancellationToken.None)
                : await this.Processor.PreviewAsync(request, "user-1", CancellationToken.None);
        }
    }
}
