using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalParkEditorialSubjectResolver
{
    private readonly IHistoricalFactRepository factRepository;
    private readonly IHistoricalRelationRepository relationRepository;

    public HistoricalParkEditorialSubjectResolver(
        IHistoricalFactRepository factRepository,
        IHistoricalRelationRepository relationRepository)
    {
        this.factRepository = factRepository;
        this.relationRepository = relationRepository;
    }

    public async Task<IReadOnlyCollection<HistoricalSubject>> LoadAsync(
        HistoricalParkEditorialScope scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        HistoricalSubjectKey[] subjectKeys = scope.CurrentSubjects
            .Select(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId))
            .ToArray();
        Task<IReadOnlyCollection<HistoricalFact>> factsTask =
            this.factRepository.GetLatestRevisionsForParkAsync(
                scope.ParkId,
                scope.CurrentSubjects,
                cancellationToken);
        Task<IReadOnlyCollection<HistoricalRelation>> relationsTask =
            this.relationRepository.GetLatestRevisionsForParkAsync(
                scope.ParkId,
                subjectKeys,
                cancellationToken);
        await Task.WhenAll(factsTask, relationsTask);
        return Merge(scope.CurrentSubjects, await factsTask, await relationsTask);
    }

    public static IReadOnlyCollection<HistoricalSubject> Merge(
        IReadOnlyCollection<HistoricalSubject> currentSubjects,
        IReadOnlyCollection<HistoricalFact> facts,
        IReadOnlyCollection<HistoricalRelation> relations)
    {
        ArgumentNullException.ThrowIfNull(currentSubjects);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(relations);
        return currentSubjects
            .Concat(facts.Select(static fact => fact.Subject))
            .Concat(relations.SelectMany(static relation => new[]
            {
                relation.Source,
                relation.Target,
            }))
            .DistinctBy(static subject => (subject.Type, subject.Id, subject.ContextParkId))
            .OrderBy(static subject => subject.Type)
            .ThenBy(static subject => subject.HistoricalLabel, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
