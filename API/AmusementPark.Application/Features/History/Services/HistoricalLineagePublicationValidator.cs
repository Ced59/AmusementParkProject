using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalLineagePublicationValidator
{
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

        string parkId = ResolveParkId(publishedCandidate);
        HistoricalSubjectKey[] candidateSubjects =
        {
            ToKey(publishedCandidate.Source),
            ToKey(publishedCandidate.Target),
        };
        IReadOnlyCollection<HistoricalRelation> currentRelations =
            await this.relationRepository.GetLatestRevisionsForParkAsync(
                parkId,
                candidateSubjects,
                cancellationToken);
        HistoricalRelation[] proposedRelations = currentRelations
            .Where(relation => relation.Id != publishedCandidate.Id && relation.IsDecisionEligible)
            .Append(publishedCandidate)
            .ToArray();
        return HistoricalLineageCycleDetector.HasIncompatibleLineageCycle(proposedRelations);
    }

    private static string ResolveParkId(HistoricalRelation relation)
    {
        string? parkId = relation.Source.ContextParkId
            ?? relation.Target.ContextParkId
            ?? (relation.Source.Type == HistoricalSubjectType.Park ? relation.Source.Id : null)
            ?? (relation.Target.Type == HistoricalSubjectType.Park ? relation.Target.Id : null);
        if (string.IsNullOrWhiteSpace(parkId))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "A historical lineage relation requires a park context.");
        }

        return parkId.Trim();
    }

    private static HistoricalSubjectKey ToKey(HistoricalSubject subject)
    {
        return new HistoricalSubjectKey(subject.Type, subject.Id, subject.ContextParkId);
    }
}
