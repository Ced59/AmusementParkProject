using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Core.Domain.History;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class GetPublicHistoricalLineageQueryHandlerTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_WhenParkRolloutGateIsClosed_ShouldNotReadRelations()
    {
        Mock<IHistoricalRelationRepository> relations = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sources = new(MockBehavior.Strict);
        Mock<IHistoricalSubjectPublicationStateReader> publication = new(MockBehavior.Strict);
        Mock<IHistoricalParkRolloutGateAccessService> gate = new(MockBehavior.Strict);
        gate.Setup(service => service.IsOpenAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        GetPublicHistoricalLineageQueryHandler handler = new(
            relations.Object,
            sources.Object,
            publication.Object,
            gate.Object);

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(
                HistoricalSubjectType.ParkItem,
                "item-1",
                "park-1"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "historical-lineage.not-found");
        relations.VerifyNoOtherCalls();
        sources.VerifyNoOtherCalls();
        publication.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPublishedRelationIsVisible_ShouldReturnExactSourcedLineage()
    {
        (HistoricalRelation Relation, HistoricalSourceReference Source) data = CreateRelation();
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
        relationRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubjectKey>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { data.Relation });
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalRelationSourceRevisionReference>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { data.Source });
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(data.Source.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { data.Source });
        publicationReader
            .Setup(reader => reader.GetPublicSubjectKeysAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<HistoricalSubjectKey>
            {
                ToKey(data.Relation.Source),
                ToKey(data.Relation.Target),
            });
        publicationReader
            .Setup(reader => reader.GetPublicParkNamesAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 1 && ids.Contains("park-1")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["park-1"] = "Parc exemple" });
        GetPublicHistoricalLineageQueryHandler handler = new(
            relationRepository.Object,
            sourceRepository.Object,
            publicationReader.Object,
            CreateOpenRolloutGate());

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(
                data.Relation.Source.Type,
                data.Relation.Source.Id,
                data.Relation.Source.ContextParkId));

        Assert.True(result.IsSuccess);
        PublicHistoricalLineageResult lineage = Assert.IsType<PublicHistoricalLineageResult>(result.Value);
        Assert.Equal(data.Relation.Source.Id, lineage.Root.Id);
        Assert.Equal("Parc exemple", Assert.IsType<PublicHistoricalLineageContextParkResult>(
            lineage.ContextPark).Name);
        Assert.Equal(2, lineage.Subjects.Count);
        PublicHistoricalRelationResult relation = Assert.Single(lineage.Relations);
        Assert.Equal(data.Relation.Id, relation.Relation.Id);
        Assert.Equal(data.Source.Id, Assert.Single(relation.Sources).Id);
        Assert.False(lineage.HasDirectedCycle);
    }

    [Fact]
    public async Task HandleAsync_WhenHistoricalOnlySubjectBelongsToHiddenPark_ShouldReturnNotFound()
    {
        (HistoricalRelation Relation, HistoricalSourceReference Source) data = CreateRelation(true);
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
        relationRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubjectKey>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { data.Relation });
        publicationReader
            .Setup(reader => reader.GetPublicSubjectKeysAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<HistoricalSubjectKey> { ToKey(data.Relation.Target) });
        GetPublicHistoricalLineageQueryHandler handler = new(
            relationRepository.Object,
            sourceRepository.Object,
            publicationReader.Object,
            CreateOpenRolloutGate());

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(
                data.Relation.Source.Type,
                data.Relation.Source.Id,
                data.Relation.Source.ContextParkId));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "historical-lineage.not-found");
        sourceRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenIdentifierIsReusedByHiddenPark_ShouldKeepLineageInsidePublicContext()
    {
        (HistoricalRelation Relation, HistoricalSourceReference Source) publicData = CreateRelation();
        (HistoricalRelation Relation, HistoricalSourceReference Source) hiddenData = CreateRelation(
            true,
            publicData.Relation.Source.Id,
            "hidden-target");
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        relationRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubjectKey>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { publicData.Relation, hiddenData.Relation });
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.Is<IReadOnlyCollection<HistoricalRelationSourceRevisionReference>>(references =>
                    references.Count == 1 && references.Single().SourceId == publicData.Source.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { publicData.Source });
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(publicData.Source.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { publicData.Source });
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
        publicationReader
            .Setup(reader => reader.GetPublicSubjectKeysAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<HistoricalSubjectKey>
            {
                ToKey(publicData.Relation.Source),
                ToKey(publicData.Relation.Target),
                ToKey(hiddenData.Relation.Target),
            });
        publicationReader
            .Setup(reader => reader.GetPublicParkNamesAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["park-1"] = "Parc exemple" });
        GetPublicHistoricalLineageQueryHandler handler = new(
            relationRepository.Object,
            sourceRepository.Object,
            publicationReader.Object,
            CreateOpenRolloutGate());

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(
                publicData.Relation.Source.Type,
                publicData.Relation.Source.Id,
                publicData.Relation.Source.ContextParkId));

        Assert.True(result.IsSuccess);
        PublicHistoricalLineageResult lineage = Assert.IsType<PublicHistoricalLineageResult>(result.Value);
        Assert.Single(lineage.Relations);
        Assert.DoesNotContain(
            lineage.Subjects,
            subject => subject.ContextParkId == hiddenData.Relation.Source.ContextParkId);
    }

    [Fact]
    public async Task HandleAsync_WhenSubjectIdentifierExceedsDomainLimit_ShouldReturnValidation()
    {
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
        GetPublicHistoricalLineageQueryHandler handler = new(
            relationRepository.Object,
            sourceRepository.Object,
            publicationReader.Object,
            CreateOpenRolloutGate());

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(HistoricalSubjectType.ParkItem, new string('x', 201)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.subject.invalid");
        relationRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenParkContextDoesNotMatchParkSubject_ShouldReturnValidation()
    {
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
        GetPublicHistoricalLineageQueryHandler handler = new(
            relationRepository.Object,
            sourceRepository.Object,
            publicationReader.Object,
            CreateOpenRolloutGate());

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(
                HistoricalSubjectType.Park,
                "real-park",
                "other-park"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.subject.invalid");
        relationRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenSupportingSourceWasWithdrawn_ShouldNotExposeLineage()
    {
        (HistoricalRelation Relation, HistoricalSourceReference Source) data = CreateRelation();
        HistoricalSourceReference withdrawnSource = CreateWithdrawnSource(data.Source);
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
        relationRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubjectKey>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { data.Relation });
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalRelationSourceRevisionReference>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { data.Source });
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { withdrawnSource });
        publicationReader
            .Setup(reader => reader.GetPublicSubjectKeysAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<HistoricalSubjectKey>
            {
                ToKey(data.Relation.Source),
                ToKey(data.Relation.Target),
            });
        GetPublicHistoricalLineageQueryHandler handler = new(
            relationRepository.Object,
            sourceRepository.Object,
            publicationReader.Object,
            CreateOpenRolloutGate());

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(
                data.Relation.Source.Type,
                data.Relation.Source.Id,
                data.Relation.Source.ContextParkId));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "historical-lineage.not-found");
    }

    [Fact]
    public async Task HandleAsync_WhenNoExplicitRelationExists_ShouldReturnNotFoundWithoutInference()
    {
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        relationRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubjectKey>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoricalRelation>());
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
        GetPublicHistoricalLineageQueryHandler handler = new(
            relationRepository.Object,
            sourceRepository.Object,
            publicationReader.Object,
            CreateOpenRolloutGate());

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(HistoricalSubjectType.ParkItem, "item-1", "park-1"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "historical-lineage.not-found");
        sourceRepository.VerifyNoOtherCalls();
        publicationReader.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenRootHasMoreThanMaximumSubjects_ShouldBoundReturnedGraph()
    {
        (HistoricalRelation Relation, HistoricalSourceReference Source)[] data = Enumerable
            .Range(1, GetPublicHistoricalLineageQueryHandler.MaximumSubjectCount + 5)
            .Select(index => CreateRelation(false, "item-root", $"item-{index}"))
            .ToArray();
        HistoricalSubjectKey rootKey = ToKey(data[0].Relation.Source);
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        relationRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubjectKey>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<HistoricalSubjectKey> subjects, int _, CancellationToken _) =>
                subjects.Contains(rootKey)
                    ? data.Select(static item => item.Relation).ToArray()
                    : Array.Empty<HistoricalRelation>());
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalRelationSourceRevisionReference>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<HistoricalRelationSourceRevisionReference> references,
                CancellationToken _) => data
                    .Where(item => references.Any(reference => reference.SourceId == item.Source.Id))
                    .Select(static item => item.Source)
                    .ToArray());
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => data
                .Where(item => ids.Contains(item.Source.Id))
                .Select(static item => item.Source)
                .ToArray());
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
        publicationReader
            .Setup(reader => reader.GetPublicSubjectKeysAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<HistoricalSubject> subjects, CancellationToken _) => subjects
                .Select(ToKey)
                .ToHashSet());
        publicationReader
            .Setup(reader => reader.GetPublicParkNamesAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["park-1"] = "Parc exemple" });
        GetPublicHistoricalLineageQueryHandler handler = new(
            relationRepository.Object,
            sourceRepository.Object,
            publicationReader.Object,
            CreateOpenRolloutGate());

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(
                HistoricalSubjectType.ParkItem,
                rootKey.Id,
                rootKey.ContextParkId));

        Assert.True(result.IsSuccess);
        PublicHistoricalLineageResult lineage = Assert.IsType<PublicHistoricalLineageResult>(result.Value);
        Assert.Equal(GetPublicHistoricalLineageQueryHandler.MaximumSubjectCount, lineage.Subjects.Count);
        Assert.Equal(GetPublicHistoricalLineageQueryHandler.MaximumSubjectCount - 1, lineage.Relations.Count);
        Assert.True(lineage.IsTruncated);
    }

    [Fact]
    public async Task HandleAsync_WhenRejectedRelationsReachSubjectCap_ShouldStillReturnLaterPublicLineage()
    {
        (HistoricalRelation Relation, HistoricalSourceReference Source)[] rejectedData = Enumerable
            .Range(1, GetPublicHistoricalLineageQueryHandler.MaximumSubjectCount - 1)
            .Select(index => CreateRelation(
                false,
                "item-root",
                $"hidden-{index}",
                Guid.Parse($"00000000-0000-0000-0000-{index:D12}")))
            .ToArray();
        (HistoricalRelation Relation, HistoricalSourceReference Source) publicData = CreateRelation(
            false,
            "item-root",
            "public-target",
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        HistoricalSubjectKey rootKey = ToKey(publicData.Relation.Source);
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        relationRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubjectKey>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<HistoricalSubjectKey> subjects, int _, CancellationToken _) =>
                subjects.Contains(rootKey)
                    ? rejectedData.Select(static item => item.Relation)
                        .Append(publicData.Relation)
                        .ToArray()
                    : Array.Empty<HistoricalRelation>());
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalRelationSourceRevisionReference>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { publicData.Source });
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { publicData.Source });
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
        publicationReader
            .Setup(reader => reader.GetPublicSubjectKeysAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<HistoricalSubjectKey>
            {
                rootKey,
                ToKey(publicData.Relation.Target),
            });
        publicationReader
            .Setup(reader => reader.GetPublicParkNamesAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["park-1"] = "Parc exemple" });
        GetPublicHistoricalLineageQueryHandler handler = new(
            relationRepository.Object,
            sourceRepository.Object,
            publicationReader.Object,
            CreateOpenRolloutGate());

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(
                HistoricalSubjectType.ParkItem,
                rootKey.Id,
                rootKey.ContextParkId));

        Assert.True(result.IsSuccess);
        PublicHistoricalLineageResult lineage = Assert.IsType<PublicHistoricalLineageResult>(result.Value);
        Assert.Equal(2, lineage.Subjects.Count);
        Assert.Equal(publicData.Relation.Id, Assert.Single(lineage.Relations).Relation.Id);
        Assert.False(lineage.IsTruncated);
    }

    [Fact]
    public async Task HandleAsync_WhenRootCurrentSubjectIsHidden_ShouldReturnNotFound()
    {
        (HistoricalRelation Relation, HistoricalSourceReference Source) data = CreateRelation();
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        relationRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubjectKey>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { data.Relation });
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
        publicationReader
            .Setup(reader => reader.GetPublicSubjectKeysAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<HistoricalSubjectKey> { ToKey(data.Relation.Target) });
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        GetPublicHistoricalLineageQueryHandler handler = new(
            relationRepository.Object,
            sourceRepository.Object,
            publicationReader.Object,
            CreateOpenRolloutGate());

        ApplicationResult<PublicHistoricalLineageResult> result = await handler.HandleAsync(
            new GetPublicHistoricalLineageQuery(
                data.Relation.Source.Type,
                data.Relation.Source.Id,
                data.Relation.Source.ContextParkId));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "historical-lineage.not-found");
        sourceRepository.VerifyNoOtherCalls();
    }

    private static (HistoricalRelation Relation, HistoricalSourceReference Source) CreateRelation(
        bool historicalOnlySource = false,
        string sourceId = "item-1",
        string targetId = "item-2",
        Guid? relationId = null)
    {
        HistoricalSubject sourceSubject = historicalOnlySource
            ? new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                "item-1",
                "Ancienne attraction",
                HistoricalSubjectPublicationPolicy.HistoricalOnly,
                "hidden-park")
            : CreateSubject(sourceId, "Ancienne attraction");
        HistoricalSubject targetSubject = CreateSubject(targetId, "Nouvelle attraction");
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(2001));
        Guid evidenceSourceId = Guid.NewGuid();
        HistoricalSourceScope[] scopes =
        {
            HistoricalSourceScope.RelationSourceIdentity,
            HistoricalSourceScope.RelationTargetIdentity,
            HistoricalSourceScope.RelationType,
            HistoricalSourceScope.Period,
        };
        HistoricalRelationSourceRevisionReference sourceReference =
            new HistoricalRelationSourceRevisionReference(
                evidenceSourceId,
                2,
                ToKey(sourceSubject),
                ToKey(targetSubject),
                HistoricalRelationType.ReplacedBy,
                period,
                HistoricalEvidencePosition.Supports,
                scopes);
        HistoricalRelation relation = new HistoricalRelation(
            relationId ?? Guid.NewGuid(),
            sourceSubject,
            targetSubject,
            HistoricalRelationType.ReplacedBy,
            HistoricalRelationDirection.Directed,
            period,
            HistoricalFactState.Verified,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            new[] { sourceReference },
            null,
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            2,
            1,
            RecordedAtUtc);
        HistoricalSourceReference source = new HistoricalSourceReference(
            evidenceSourceId,
            2,
            HistoricalSourceType.OfficialWebsite,
            "Historique officiel",
            "Parc exemple",
            "https://example.com/history",
            null,
            new DateOnly(2001, 1, 1),
            new DateOnly(2026, 9, 27),
            "fr",
            null,
            scopes,
            null,
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            RecordedAtUtc);
        return (relation, source);
    }

    private static HistoricalSubject CreateSubject(string id, string label)
    {
        return new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            id,
            label,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
    }

    private static HistoricalSourceReference CreateWithdrawnSource(HistoricalSourceReference source)
    {
        return new HistoricalSourceReference(
            source.Id,
            source.Revision + 1,
            source.Type,
            source.Title,
            source.PublisherOrAuthor,
            source.Url,
            source.BibliographicReference,
            source.PublishedOn,
            source.AccessedOn,
            source.LanguageCode,
            source.ArchiveUrl,
            source.Scopes,
            source.AdminNote,
            HistoricalSourceAccessibility.Withdrawn,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            source.RecordedAtUtc.AddMinutes(1));
    }

    private static HistoricalSubjectKey ToKey(HistoricalSubject subject)
    {
        return new HistoricalSubjectKey(subject.Type, subject.Id, subject.ContextParkId);
    }

    private static IHistoricalParkRolloutGateAccessService CreateOpenRolloutGate()
    {
        Mock<IHistoricalParkRolloutGateAccessService> gate = new(MockBehavior.Strict);
        gate.Setup(service => service.IsOpenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return gate.Object;
    }
}
