using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

public sealed class ParkFitPilotObservation
{
    private const int MaximumMethodVersionLength = 40;
    private const int MaximumQualityIssueCount = 16;

    private ParkFitPilotObservation(
        ParkFitPilotEventKind eventKind,
        ParkFitPilotResultBand? resultBand,
        ParkFitPilotUnknownLevel? unknownLevel,
        ParkFitPilotDurationBand? durationBand,
        ParkFitPilotFailureKind? failureKind,
        ParkFitPilotComparisonSize? comparisonSize,
        string? methodVersion,
        IReadOnlyCollection<ParkFitDataQualityIssue> qualityIssues)
    {
        this.EventKind = eventKind;
        this.ResultBand = resultBand;
        this.UnknownLevel = unknownLevel;
        this.DurationBand = durationBand;
        this.FailureKind = failureKind;
        this.ComparisonSize = comparisonSize;
        this.MethodVersion = methodVersion;
        this.QualityIssues = qualityIssues;
    }

    public ParkFitPilotEventKind EventKind { get; }

    public ParkFitPilotResultBand? ResultBand { get; }

    public ParkFitPilotUnknownLevel? UnknownLevel { get; }

    public ParkFitPilotDurationBand? DurationBand { get; }

    public ParkFitPilotFailureKind? FailureKind { get; }

    public ParkFitPilotComparisonSize? ComparisonSize { get; }

    public string? MethodVersion { get; }

    public IReadOnlyCollection<ParkFitDataQualityIssue> QualityIssues { get; }

    public static ParkFitPilotObservation Create(
        ParkFitPilotEventKind eventKind,
        ParkFitPilotResultBand? resultBand,
        ParkFitPilotUnknownLevel? unknownLevel,
        ParkFitPilotDurationBand? durationBand,
        ParkFitPilotFailureKind? failureKind,
        ParkFitPilotComparisonSize? comparisonSize,
        string? methodVersion,
        IEnumerable<ParkFitDataQualityIssue>? qualityIssues)
    {
        if (!Enum.IsDefined(eventKind))
        {
            throw new ArgumentOutOfRangeException(nameof(eventKind));
        }

        if ((resultBand.HasValue && !Enum.IsDefined(resultBand.Value))
            || (unknownLevel.HasValue && !Enum.IsDefined(unknownLevel.Value))
            || (durationBand.HasValue && !Enum.IsDefined(durationBand.Value))
            || (failureKind.HasValue && !Enum.IsDefined(failureKind.Value))
            || (comparisonSize.HasValue && !Enum.IsDefined(comparisonSize.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(eventKind));
        }

        List<ParkFitDataQualityIssue> requestedIssues = (qualityIssues ?? []).ToList();
        if (requestedIssues.Count > MaximumQualityIssueCount
            || requestedIssues.Any(static issue => !Enum.IsDefined(issue)))
        {
            throw new ArgumentOutOfRangeException(nameof(qualityIssues));
        }

        List<ParkFitDataQualityIssue> normalizedIssues = requestedIssues
            .Distinct()
            .OrderBy(static issue => issue)
            .ToList();
        string? normalizedVersion = string.IsNullOrWhiteSpace(methodVersion)
            ? null
            : methodVersion.Trim();
        if (normalizedVersion?.Length > MaximumMethodVersionLength)
        {
            throw new ArgumentOutOfRangeException(nameof(methodVersion));
        }

        bool valid = eventKind switch
        {
            ParkFitPilotEventKind.SearchStarted => resultBand is null
                && unknownLevel is null
                && durationBand is null
                && failureKind is null
                && comparisonSize is null
                && normalizedVersion is null
                && normalizedIssues.Count == 0,
            ParkFitPilotEventKind.SearchCompleted => resultBand.HasValue
                && unknownLevel.HasValue
                && durationBand.HasValue
                && failureKind is null
                && comparisonSize is null
                && normalizedVersion is not null,
            ParkFitPilotEventKind.SearchFailed => resultBand is null
                && unknownLevel is null
                && durationBand.HasValue
                && failureKind.HasValue
                && comparisonSize is null
                && normalizedVersion is null
                && normalizedIssues.Count == 0,
            ParkFitPilotEventKind.ExplanationViewed => resultBand is null
                && unknownLevel is null
                && durationBand is null
                && failureKind is null
                && comparisonSize is null
                && normalizedVersion is null
                && normalizedIssues.Count == 0,
            ParkFitPilotEventKind.ComparisonOpened => resultBand is null
                && unknownLevel is null
                && durationBand is null
                && failureKind is null
                && comparisonSize.HasValue
                && normalizedVersion is null
                && normalizedIssues.Count == 0,
            _ => false,
        };
        if (!valid)
        {
            throw new ArgumentException("The Park Fit pilot observation dimensions are inconsistent.");
        }

        return new ParkFitPilotObservation(
            eventKind,
            resultBand,
            unknownLevel,
            durationBand,
            failureKind,
            comparisonSize,
            normalizedVersion,
            normalizedIssues);
    }
}
