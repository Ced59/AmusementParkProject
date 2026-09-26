namespace AmusementPark.Core.Domain.History;

public sealed record ParkHistoricalSnapshot
{
    public ParkHistoricalSnapshot(
        string parkId,
        HistoricalInstant requestedInstant,
        IReadOnlyCollection<HistoricalSubjectSnapshot> subjects,
        HistoricalCoverage coverage,
        IReadOnlyCollection<HistoricalAmbiguity> ambiguities,
        string methodologyVersion)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        string normalizedMethodologyVersion = methodologyVersion?.Trim() ?? string.Empty;
        if (normalizedParkId.Length == 0 || normalizedParkId.Length > 200)
        {
            throw new ArgumentException("A park historical snapshot requires a valid park identifier.", nameof(parkId));
        }

        if (normalizedMethodologyVersion.Length == 0 || normalizedMethodologyVersion.Length > 100)
        {
            throw new ArgumentException(
                "A park historical snapshot requires a valid methodology version.",
                nameof(methodologyVersion));
        }

        ArgumentNullException.ThrowIfNull(requestedInstant);
        ArgumentNullException.ThrowIfNull(subjects);
        ArgumentNullException.ThrowIfNull(coverage);
        ArgumentNullException.ThrowIfNull(ambiguities);
        HistoricalSubjectSnapshot[] normalizedSubjects = subjects
            .OrderBy(static subject => subject.Subject.Type)
            .ThenBy(static subject => subject.Subject.HistoricalLabel, StringComparer.Ordinal)
            .ThenBy(static subject => subject.Subject.Id, StringComparer.Ordinal)
            .ToArray();
        int distinctSubjectCount = normalizedSubjects
            .Select(static subject => (subject.Subject.Type, subject.Subject.Id))
            .Distinct()
            .Count();
        if (distinctSubjectCount != normalizedSubjects.Length)
        {
            throw new ArgumentException("A subject can occur only once in a park historical snapshot.", nameof(subjects));
        }

        if (coverage.TotalSubjectCount != normalizedSubjects.Length)
        {
            throw new ArgumentException(
                "Historical coverage must describe every snapshot subject.",
                nameof(coverage));
        }

        HistoricalAmbiguity[] normalizedAmbiguities = ambiguities
            .OrderBy(static ambiguity => ambiguity.Subject.Type)
            .ThenBy(static ambiguity => ambiguity.Subject.HistoricalLabel, StringComparer.Ordinal)
            .ThenBy(static ambiguity => ambiguity.Subject.Id, StringComparer.Ordinal)
            .ThenBy(static ambiguity => ambiguity.AttributeKind)
            .ThenBy(static ambiguity => ambiguity.Code)
            .ToArray();
        HashSet<(HistoricalSubjectType Type, string Id)> subjectKeys = normalizedSubjects
            .Select(static subject => (subject.Subject.Type, subject.Subject.Id))
            .ToHashSet();
        bool hasForeignAmbiguity = normalizedAmbiguities.Any(ambiguity => !subjectKeys.Contains(
            (ambiguity.Subject.Type, ambiguity.Subject.Id)));
        if (hasForeignAmbiguity)
        {
            throw new ArgumentException(
                "A historical ambiguity must target a snapshot subject.",
                nameof(ambiguities));
        }

        this.ParkId = normalizedParkId;
        this.RequestedInstant = requestedInstant;
        this.Subjects = Array.AsReadOnly(normalizedSubjects);
        this.Coverage = coverage;
        this.Ambiguities = Array.AsReadOnly(normalizedAmbiguities);
        this.MethodologyVersion = normalizedMethodologyVersion;
    }

    public string ParkId { get; }

    public HistoricalInstant RequestedInstant { get; }

    public IReadOnlyList<HistoricalSubjectSnapshot> Subjects { get; }

    public HistoricalCoverage Coverage { get; }

    public IReadOnlyList<HistoricalAmbiguity> Ambiguities { get; }

    public string MethodologyVersion { get; }
}
