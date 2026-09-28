using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalLineagePublicationValidator
{
    private const int MaximumRelationsPerLevel = 200;
    private const int MaximumSubjectCount = 200;

    private readonly IHistoricalRelationRepository relationRepository;

    public HistoricalLineagePublicationValidator(IHistoricalRelationRepository relationRepository)
    {
        this.relationRepository = relationRepository;
    }

    public async Task<bool> WouldCreateCycleAsync(
        HistoricalRelation publishedCandidate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publishedCandidate);
        if (!publishedCandidate.IsDecisionEligible
            || !HistoricalLineageCycleDetector.IsLineageType(publishedCandidate.Type))
        {
            return false;
        }

        HistoricalSubjectKey candidateSource = ToKey(publishedCandidate.Source);
        HistoricalSubjectKey candidateTarget = ToKey(publishedCandidate.Target);
        HashSet<HistoricalSubjectKey> visited = new HashSet<HistoricalSubjectKey>
        {
            candidateTarget,
        };
        HashSet<HistoricalSubjectKey> frontier = new HashSet<HistoricalSubjectKey>
        {
            candidateTarget,
        };
        while (frontier.Count > 0)
        {
            IReadOnlyCollection<HistoricalRelation> currentRelations =
                await this.relationRepository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                    frontier,
                    MaximumRelationsPerLevel,
                    cancellationToken);
            HashSet<HistoricalSubjectKey> next = new HashSet<HistoricalSubjectKey>();
            foreach (HistoricalRelation relation in currentRelations.Where(relation =>
                         relation.Id != publishedCandidate.Id
                         && relation.IsDecisionEligible
                         && relation.Direction == HistoricalRelationDirection.Directed
                         && HistoricalLineageCycleDetector.IsLineageType(relation.Type)))
            {
                HistoricalSubjectKey source = ToKey(relation.Source);
                if (!frontier.Contains(source))
                {
                    continue;
                }

                HistoricalSubjectKey target = ToKey(relation.Target);
                if (target == candidateSource)
                {
                    return true;
                }

                if (visited.Add(target))
                {
                    next.Add(target);
                }
            }

            if (currentRelations.Count >= MaximumRelationsPerLevel
                || visited.Count > MaximumSubjectCount)
            {
                return true;
            }

            frontier = next;
        }

        return false;
    }

    private static HistoricalSubjectKey ToKey(HistoricalSubject subject)
    {
        return new HistoricalSubjectKey(subject.Type, subject.Id, subject.ContextParkId);
    }
}
