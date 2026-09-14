export type ParkFitPilotEventKind =
  | 'SearchStarted'
  | 'SearchCompleted'
  | 'SearchFailed'
  | 'ExplanationViewed'
  | 'ComparisonOpened';

export type ParkFitPilotResultBand = 'None' | 'One' | 'TwoToFour' | 'FiveOrMore';
export type ParkFitPilotUnknownLevel = 'None' | 'Limited' | 'Significant';
export type ParkFitPilotDurationBand =
  | 'UnderHalfSecond'
  | 'UnderOneAndHalfSeconds'
  | 'UnderThreeSeconds'
  | 'ThreeSecondsOrMore';
export type ParkFitPilotFailureKind = 'Validation' | 'RateLimited' | 'Technical';
export type ParkFitPilotComparisonSize = 'Two' | 'Three' | 'Four';

export interface ParkFitPilotObservation {
  readonly eventKind: ParkFitPilotEventKind;
  readonly resultBand?: ParkFitPilotResultBand | null;
  readonly unknownLevel?: ParkFitPilotUnknownLevel | null;
  readonly durationBand?: ParkFitPilotDurationBand | null;
  readonly failureKind?: ParkFitPilotFailureKind | null;
  readonly comparisonSize?: ParkFitPilotComparisonSize | null;
  readonly methodVersion?: string | null;
  readonly qualityIssues?: readonly string[];
}
