using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalParkRolloutGateAssessmentService : IHistoricalParkRolloutGateAssessmentService
{
    private readonly IParkHistoricalSnapshotBuilder snapshotBuilder;
    private readonly HistoricalParkRolloutGateEvaluator gateEvaluator;
    private readonly IHistoricalSourceRepository sourceRepository;

    public HistoricalParkRolloutGateAssessmentService(
        IParkHistoricalSnapshotBuilder snapshotBuilder,
        HistoricalParkRolloutGateEvaluator gateEvaluator,
        IHistoricalSourceRepository sourceRepository)
    {
        this.snapshotBuilder = snapshotBuilder;
        this.gateEvaluator = gateEvaluator;
        this.sourceRepository = sourceRepository;
    }

    public async Task<HistoricalParkRolloutGate> AssessAsync(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> subjects,
        IReadOnlyCollection<HistoricalFact> facts,
        CancellationToken cancellationToken)
    {
        HistoricalFact[] publicFacts = ValidateAndFilterPublicFacts(parkId, subjects, facts);
        IReadOnlySet<(Guid SourceId, int Revision)> admissibleSourceRevisions =
            await this.LoadCurrentlyAdmissibleSourceRevisionsAsync(publicFacts, cancellationToken);
        return this.AssessWithAdmissibleSources(
            parkId.Trim(),
            subjects,
            publicFacts,
            admissibleSourceRevisions);
    }

    public async Task<IReadOnlyDictionary<string, HistoricalParkRolloutGate>> AssessManyAsync(
        IReadOnlyCollection<HistoricalParkRolloutGateAssessmentRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Count == 0)
        {
            return new Dictionary<string, HistoricalParkRolloutGate>(StringComparer.Ordinal);
        }

        List<(string ParkId, IReadOnlyCollection<HistoricalSubject> Subjects, HistoricalFact[] Facts)>
            normalizedRequests = new(requests.Count);
        HashSet<string> parkIds = new(StringComparer.Ordinal);
        foreach (HistoricalParkRolloutGateAssessmentRequest request in requests)
        {
            ArgumentNullException.ThrowIfNull(request);
            HistoricalFact[] publicFacts = ValidateAndFilterPublicFacts(
                request.ParkId,
                request.Subjects,
                request.Facts);
            string parkId = request.ParkId.Trim();
            if (!parkIds.Add(parkId))
            {
                throw new ArgumentException(
                    $"The park '{parkId}' can only be assessed once per batch.",
                    nameof(requests));
            }

            normalizedRequests.Add((parkId, request.Subjects, publicFacts));
        }

        IReadOnlySet<(Guid SourceId, int Revision)> admissibleSourceRevisions =
            await this.LoadCurrentlyAdmissibleSourceRevisionsAsync(
                normalizedRequests.SelectMany(static request => request.Facts).ToArray(),
                cancellationToken);
        Dictionary<string, HistoricalParkRolloutGate> assessments =
            new(StringComparer.Ordinal);
        foreach ((string parkId, IReadOnlyCollection<HistoricalSubject> subjects, HistoricalFact[] facts)
                 in normalizedRequests)
        {
            assessments.Add(
                parkId,
                this.AssessWithAdmissibleSources(
                    parkId,
                    subjects,
                    facts,
                    admissibleSourceRevisions));
        }

        return assessments;
    }

    private HistoricalParkRolloutGate AssessWithAdmissibleSources(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> subjects,
        IReadOnlyCollection<HistoricalFact> publicFacts,
        IReadOnlySet<(Guid SourceId, int Revision)> admissibleSourceRevisions)
    {
        int[] candidateYears = publicFacts
            .Where(static fact => fact.Importance == HistoricalImportance.Major)
            .SelectMany(static fact => ResolveBoundaryYears(fact.Period))
            .Distinct()
            .Order()
            .ToArray();
        int[] indexableKeyYears = candidateYears
            .Where(year => HistoricalSnapshotSeoEligibilityEvaluator.IsIndexableKeyYear(
                this.snapshotBuilder.Build(
                    parkId,
                    HistoricalInstant.ForYear(year),
                    subjects,
                    publicFacts),
                publicFacts))
            .ToArray();
        return this.gateEvaluator.Evaluate(
            publicFacts,
            indexableKeyYears,
            admissibleSourceRevisions);
    }

    public Task<HistoricalParkRolloutGate> AssessPublicParkAsync(
        HistoricalParkEditorialScope scope,
        IReadOnlyCollection<HistoricalFact> facts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(facts);
        if (!scope.IsPublicPark)
        {
            return this.AssessAsync(
                scope.ParkId,
                Array.Empty<HistoricalSubject>(),
                Array.Empty<HistoricalFact>(),
                cancellationToken);
        }

        HashSet<HistoricalSubjectKey> publicCurrentSubjectKeys = scope.PublicCurrentSubjects
            .Select(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId))
            .ToHashSet();
        HistoricalFact[] publicFacts = facts
            .Where(fact => IsPublicFact(fact, scope.ParkId, publicCurrentSubjectKeys))
            .ToArray();
        HistoricalSubject[] publicSubjects = scope.PublicCurrentSubjects
            .Concat(publicFacts
                .Where(static fact => fact.Subject.PublicationPolicy
                    == HistoricalSubjectPublicationPolicy.HistoricalOnly)
                .Select(static fact => fact.Subject))
            .DistinctBy(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId))
            .ToArray();
        return this.AssessAsync(scope.ParkId, publicSubjects, publicFacts, cancellationToken);
    }

    private async Task<IReadOnlySet<(Guid SourceId, int Revision)>>
        LoadCurrentlyAdmissibleSourceRevisionsAsync(
            IReadOnlyCollection<HistoricalFact> facts,
            CancellationToken cancellationToken)
    {
        HistoricalSourceRevisionReference[] references = facts
            .SelectMany(static fact => fact.SourceReferences)
            .DistinctBy(static reference => (reference.SourceId, reference.Revision))
            .ToArray();
        if (references.Length == 0)
        {
            return new HashSet<(Guid SourceId, int Revision)>();
        }

        IReadOnlyCollection<HistoricalSourceReference> resolvedRevisions =
            await this.sourceRepository.GetRevisionsAsync(references, cancellationToken);
        IReadOnlyCollection<HistoricalSourceReference> latestRevisions = resolvedRevisions.Count == 0
            ? Array.Empty<HistoricalSourceReference>()
            : await this.sourceRepository.GetLatestRevisionsAsync(
                resolvedRevisions.Select(static source => source.Id).Distinct().ToArray(),
                cancellationToken);
        return HistoricalRelationEvidenceValidator.FilterCurrentlyAdmissiblePublicSources(
                resolvedRevisions,
                latestRevisions)
            .Select(static source => (source.Id, source.Revision))
            .ToHashSet();
    }

    private static HistoricalFact[] ValidateAndFilterPublicFacts(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> subjects,
        IReadOnlyCollection<HistoricalFact> facts)
    {
        if (string.IsNullOrWhiteSpace(parkId))
        {
            throw new ArgumentException("A park identifier is required.", nameof(parkId));
        }

        ArgumentNullException.ThrowIfNull(subjects);
        ArgumentNullException.ThrowIfNull(facts);
        return facts
            .Where(static fact => fact.IsDecisionEligible
                && fact.Subject.PublicationPolicy != HistoricalSubjectPublicationPolicy.Suppressed)
            .ToArray();
    }

    private static bool IsPublicFact(
        HistoricalFact fact,
        string parkId,
        IReadOnlySet<HistoricalSubjectKey> publicCurrentSubjectKeys)
    {
        if (!fact.IsDecisionEligible
            || fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.Suppressed)
        {
            return false;
        }

        HistoricalSubjectKey key = new(
            fact.Subject.Type,
            fact.Subject.Id,
            fact.Subject.ContextParkId);
        return fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly
            ? string.Equals(fact.Subject.ContextParkId, parkId, StringComparison.Ordinal)
            : publicCurrentSubjectKeys.Contains(key);
    }

    private static IEnumerable<int> ResolveBoundaryYears(HistoricalPeriod period)
    {
        if (period.Start is not null)
        {
            yield return period.Start.Year;
        }

        if (period.End is not null && period.End.Year != period.Start?.Year)
        {
            yield return period.End.Year;
        }
    }
}
