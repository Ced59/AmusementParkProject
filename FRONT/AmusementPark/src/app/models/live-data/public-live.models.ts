export type PublicLiveAvailability = 'NoObservation' | 'Current' | 'Expired' | 'Unavailable';

export type PublicLiveOperationalStatus =
  | 'Open'
  | 'Closed'
  | 'TemporarilyClosed'
  | 'Delayed'
  | 'Down'
  | 'WeatherClosed'
  | 'Maintenance'
  | 'OperatingWithLimitations'
  | 'Unknown'
  | 'NotOperatingToday'
  | 'Removed';

export type PublicLiveQueueKind =
  | 'Standby'
  | 'SingleRider'
  | 'ReturnTime'
  | 'PaidReturnTime'
  | 'BoardingGroup'
  | 'PaidStandby';

export type PublicLiveQueueAvailability =
  | 'Unspecified'
  | 'Available'
  | 'TemporarilyFull'
  | 'Finished'
  | 'Paused'
  | 'Closed'
  | 'Unknown';

export interface PublicLiveQueue {
  readonly kind: PublicLiveQueueKind;
  readonly waitTimeMinutes: number | null;
  readonly isEstimated: boolean;
  readonly availability: PublicLiveQueueAvailability;
  readonly returnStartUtc: string | null;
  readonly returnEndUtc: string | null;
  readonly currentGroupStart: number | null;
  readonly currentGroupEnd: number | null;
  readonly nextAllocationUtc: string | null;
  readonly priceMinorUnits: number | null;
  readonly currencyCode: string | null;
}

export interface PublicLiveSource {
  readonly id: string;
  readonly displayName: string;
  readonly type: string;
  readonly attributionText: string;
  readonly attributionUrl: string;
}

export interface PublicLiveTarget {
  readonly targetId: string;
  readonly targetType: 'Park' | 'ParkItem';
  readonly displayName: string;
  readonly parkId: string;
  readonly parkDisplayName: string;
  readonly availability: PublicLiveAvailability;
  readonly status: PublicLiveOperationalStatus | null;
  readonly queues: readonly PublicLiveQueue[];
  readonly asOfUtc: string;
  readonly observedAtUtc: string | null;
  readonly receivedAtUtc: string | null;
  readonly ageSeconds: number | null;
  readonly freshness: string | null;
  readonly expiresAtUtc: string | null;
  readonly source: PublicLiveSource | null;
  readonly confidence: string | null;
}

export interface PublicParkLiveItems {
  readonly parkId: string;
  readonly parkDisplayName: string;
  readonly asOfUtc: string;
  readonly items: readonly PublicLiveTarget[];
}

export type PublicLiveHistoryDataStatus = 'Unavailable' | 'Insufficient' | 'Sparse' | 'Usable';

export interface PublicLiveHistoryExclusions {
  readonly duplicateObservations: number;
  readonly outsideActiveWindow: number;
  readonly nonOperatingStatus: number;
  readonly missingStandbyWait: number;
  readonly total: number;
}

export interface PublicLiveHistoryHour {
  readonly localHour: number;
  readonly dataStatus: PublicLiveHistoryDataStatus;
  readonly expectedObservationCount: number;
  readonly observationCount: number;
  readonly usableWaitCount: number;
  readonly daysCovered: number;
  readonly comparableDays: number;
  readonly coveragePercent: number;
  readonly robustMinimumMinutes: number | null;
  readonly firstQuartileMinutes: number | null;
  readonly medianMinutes: number | null;
  readonly thirdQuartileMinutes: number | null;
  readonly robustMaximumMinutes: number | null;
}

export interface PublicLiveHistory {
  readonly targetId: string;
  readonly displayName: string;
  readonly parkId: string;
  readonly parkDisplayName: string;
  readonly fromUtc: string;
  readonly toUtc: string;
  readonly timeZoneId: string;
  readonly dataStatus: PublicLiveHistoryDataStatus;
  readonly expectedObservationCount: number;
  readonly observationCount: number;
  readonly usableWaitCount: number;
  readonly daysCovered: number;
  readonly comparableDays: number;
  readonly coveragePercent: number;
  readonly truncatedObservationCount: number;
  readonly exclusions: PublicLiveHistoryExclusions;
  readonly hours: readonly PublicLiveHistoryHour[];
  readonly source: PublicLiveSource;
}

export interface PublicLiveForecast {
  readonly targetDisplayName: string;
  readonly parkDisplayName: string;
  readonly timeZoneId: string;
  readonly forecastFromUtc: string;
  readonly forecastToUtc: string;
  readonly calculatedAtUtc: string;
  readonly expectedWaitMinutes: number;
  readonly lowerBoundMinutes: number;
  readonly upperBoundMinutes: number;
  readonly trainingDayCount: number;
  readonly studyVersion: string;
  readonly method: string;
  readonly intervalMethod: string;
  readonly meanAbsoluteErrorMinutes: number;
  readonly intervalCoveragePercent: number;
  readonly evaluationFromUtc: string;
  readonly evaluationToUtc: string;
  readonly evaluationPointCount: number;
  readonly source: PublicLiveSource;
}
