export type ParkFitPilotSignal =
  | 'AwaitingObservations'
  | 'NeedsAttention'
  | 'Monitor'
  | 'Encouraging';

export interface ParkFitPilotMetricsQuery {
  readonly fromUtc?: string | null;
  readonly toUtc?: string | null;
}

export interface ParkFitPilotHealth {
  readonly completionRatePercent: number;
  readonly noResultRatePercent: number;
  readonly significantUnknownRatePercent: number;
  readonly explanationOpenRatePercent: number;
  readonly comparisonOpenRatePercent: number;
  readonly signal: ParkFitPilotSignal;
  readonly requiresQualitativeReview: boolean;
}

export interface ParkFitPilotDailyMetrics {
  readonly date: string;
  readonly eventCounts: Readonly<Record<string, number>>;
  readonly sourceReports: number;
  readonly outdatedSourceReports: number;
}

export interface ParkFitPilotMetricsResult {
  readonly generatedAtUtc: string;
  readonly fromUtc: string;
  readonly toUtc: string;
  readonly searchesStarted: number;
  readonly searchesCompleted: number;
  readonly searchesFailed: number;
  readonly searchesAbandoned: number;
  readonly explanationsViewed: number;
  readonly comparisonsOpened: number;
  readonly sourceReports: number;
  readonly outdatedSourceReports: number;
  readonly health: ParkFitPilotHealth;
  readonly resultBandCounts: Readonly<Record<string, number>>;
  readonly unknownLevelCounts: Readonly<Record<string, number>>;
  readonly durationBandCounts: Readonly<Record<string, number>>;
  readonly failureKindCounts: Readonly<Record<string, number>>;
  readonly comparisonSizeCounts: Readonly<Record<string, number>>;
  readonly qualityIssueCounts: Readonly<Record<string, number>>;
  readonly zeroResultQualityIssueCounts: Readonly<Record<string, number>>;
  readonly methodVersionCounts: Readonly<Record<string, number>>;
  readonly daily: readonly ParkFitPilotDailyMetrics[];
}
