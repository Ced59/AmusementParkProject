export type ParkFitAttractionType =
  | 'RollerCoaster'
  | 'WaterRide'
  | 'FlatRide'
  | 'DarkRide'
  | 'FamilyRide'
  | 'ThrillRide'
  | 'TransportRide'
  | 'WalkThrough'
  | 'Playground'
  | 'InteractiveExperience'
  | 'Cinema'
  | 'DropTower'
  | 'Game'
  | 'MeetAndGreet'
  | 'ObservationRide';

export type ParkFitUnknownDataPolicy = 'KeepWithWarning' | 'ExcludeUnknown' | 'KnownOnly';

export interface ParkFitSearchMemberCriteria {
  heightCentimeters: number | null;
  minimumAgeYears: number | null;
  maximumAgeYears: number | null;
  canBeAccompanied: boolean;
  companionMinimumAgeYears: number | null;
  companionMaximumAgeYears: number | null;
}

export interface ParkFitSearchRequest {
  evaluationDate: string;
  members: ParkFitSearchMemberCriteria[];
  preferredAttractionTypes: ParkFitAttractionType[];
  preferIndoor: boolean;
  countryCode: string | null;
  unknownDataPolicy: ParkFitUnknownDataPolicy;
  maximumResults: number;
}

export interface ParkFitScoreComponent {
  kind: string;
  state: string;
  value: number | null;
  coveragePercent: number;
  confidence: string;
  baseWeightPercent: number;
  applicableWeightPercent: number;
  knownScoreWeightPercent: number | null;
  contribution: number | null;
  reasons: string[];
}

export interface ParkFitCriticalSource {
  kind: string;
  url: string | null;
  reference: string | null;
  languageCode: string | null;
  collectedAtUtc: string | null;
  verifiedAtUtc: string | null;
  confidence: string;
  summaries: ParkFitLocalizedText[];
}

export interface ParkFitLocalizedText {
  languageCode: string;
  value: string | null;
}

export interface ParkFitSearchMemberSummary {
  memberNumber: number;
  compatibleAloneAttractionCount: number;
  compatibleWithCompanionAttractionCount: number;
  incompatibleAttractionCount: number;
  unknownAttractionCount: number;
  notApplicableAttractionCount: number;
}

export interface ParkFitSearchPark {
  parkId: string;
  parkName: string;
  countryCode: string | null;
  parkType: string | null;
  scoreState: string;
  comparativeScore: number | null;
  rawKnownScore: number | null;
  coveragePercent: number;
  knownWeightPercent: number;
  scoreCeilingPercent: number | null;
  confidence: string;
  dateAvailabilityState: string;
  unknownCount: number;
  everyoneTogetherAttractionCount: number;
  splitRequiredAttractionCount: number;
  partialAttractionCount: number;
  noCompatibleMemberAttractionCount: number;
  unknownAttractionCount: number;
  dataQualityStatus: string;
  dataQualityCoveragePercent: number;
  lastVerifiedAtUtc: string | null;
  reasons: string[];
  components: ParkFitScoreComponent[];
  memberSummaries: ParkFitSearchMemberSummary[];
  criticalSources: ParkFitCriticalSource[];
}

export interface ParkFitSearchResponse {
  methodVersion: string;
  evaluationDate: string;
  evaluatedAtUtc: string;
  totalCandidateCount: number;
  inspectedCandidateCount: number;
  qualityEligibleCandidateCount: number;
  qualityRejectedCandidateCount: number;
  candidatePoolTruncated: boolean;
  qualityStatusCounts: Record<string, number>;
  qualityIssueCounts: Record<string, number>;
  parks: ParkFitSearchPark[];
}
