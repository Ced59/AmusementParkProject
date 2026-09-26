namespace AmusementPark.Core.Domain.History;

internal sealed class HistoricalAmbiguityBuilder
{
    internal IReadOnlyList<HistoricalAmbiguity> Build(
        IReadOnlyCollection<HistoricalSubjectSnapshot> subjects)
    {
        List<HistoricalAmbiguity> ambiguities = new List<HistoricalAmbiguity>();
        foreach (HistoricalSubjectSnapshot subject in subjects)
        {
            foreach (HistoricalAttributeSnapshot attribute in subject.Attributes)
            {
                ambiguities.AddRange(attribute.Reasons
                    .Where(static reason => HistoricalAmbiguity.IsAmbiguityCode(reason.Code))
                    .Select(reason => new HistoricalAmbiguity(
                        subject.Subject,
                        reason.Code,
                        attribute.Kind,
                        reason.FactIds)));
            }

            foreach (HistoricalSnapshotReason reason in subject.Reasons.Where(
                         static reason => HistoricalAmbiguity.IsAmbiguityCode(reason.Code)))
            {
                HistoricalSnapshotReason[] attributedReasons = subject.Attributes
                    .SelectMany(static attribute => attribute.Reasons)
                    .Where(attributeReason => attributeReason.Code == reason.Code)
                    .ToArray();
                Guid[] remainingFactIds = reason.FactIds
                    .Except(attributedReasons.SelectMany(static attributeReason => attributeReason.FactIds))
                    .ToArray();
                if (remainingFactIds.Length == 0
                    && (attributedReasons.Length > 0 || reason.FactIds.Count > 0))
                {
                    continue;
                }

                ambiguities.Add(new HistoricalAmbiguity(
                    subject.Subject,
                    reason.Code,
                    null,
                    remainingFactIds));
            }
        }

        return ambiguities
            .OrderBy(static ambiguity => ambiguity.Subject.Type)
            .ThenBy(static ambiguity => ambiguity.Subject.HistoricalLabel, StringComparer.Ordinal)
            .ThenBy(static ambiguity => ambiguity.Subject.Id, StringComparer.Ordinal)
            .ThenBy(static ambiguity => ambiguity.AttributeKind)
            .ThenBy(static ambiguity => ambiguity.Code)
            .ThenBy(static ambiguity => string.Join(',', ambiguity.FactIds))
            .ToArray();
    }

}
