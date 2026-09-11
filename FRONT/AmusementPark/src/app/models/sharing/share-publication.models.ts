export type ShareContentField =
  | 'PublicDisplayName'
  | 'Avatar'
  | 'RideCount'
  | 'TemporalRatings'
  | 'GlobalRatings'
  | 'PublicCaption'
  | 'GeographicStatistics'
  | 'MissedItems';

export interface ShareContentPolicyPreview {
  schemaVersion: number;
  datePrecision: string;
  includedFields: ShareContentField[];
}

export interface PersonalRankingSharePreviewItem {
  targetType: string;
  targetName: string;
  parkName?: string | null;
  parkItemCategory?: string | null;
  parkItemType?: string | null;
  rating: number;
}

export interface PersonalRankingSharePreview {
  displayName?: string | null;
  avatarUrl?: string | null;
  statistics?: PersonalRankingShareStatistics | null;
  ratings: PersonalRankingSharePreviewItem[];
  isTruncated: boolean;
}

export interface PersonalRankingShareStatBucket {
  key?: string | null;
  label: string;
  count: number;
  averageRating: number;
}

export interface PersonalRankingShareStatistics {
  totalRatings: number;
  averageRating: number;
  highestRating: number;
  lowestRating: number;
  byPark: PersonalRankingShareStatBucket[];
  byTargetType: PersonalRankingShareStatBucket[];
  byParkItemCategory: PersonalRankingShareStatBucket[];
}

export interface SharePublicationPreview {
  publicationType: string;
  sourceVersion: number;
  approvalToken: string;
  contentPolicy: ShareContentPolicyPreview;
  personalRanking?: PersonalRankingSharePreview | null;
  visitRecap?: VisitRecapSharePreview | null;
  yearRecap?: YearRecapSharePreview | null;
  passportProfile?: PassportProfileSharePreview | null;
}

export interface SharePublicationPreviewRequest {
  publicationType: string;
  sourceId?: string | null;
  datePrecision: string;
  includedFields: ShareContentField[];
  visitRecap?: VisitRecapShareInput | null;
  yearRecap?: YearRecapShareInput | null;
  passportProfile?: PassportProfileShareInput | null;
}

export interface SharePublicationPublishRequest {
  publicationType: string;
  sourceId?: string | null;
  approvedSourceVersion: number;
  approvedPolicySchemaVersion: number;
  approvedDatePrecision: string;
  approvedIncludedFields: ShareContentField[];
  approvalToken: string;
  visitRecap?: VisitRecapShareInput | null;
  yearRecap?: YearRecapShareInput | null;
  passportProfile?: PassportProfileShareInput | null;
}

export interface SharePublicationSettings {
  isPublic: boolean;
  shareId?: string | null;
  publishedAtUtc?: string | null;
  policySchemaVersion?: number | null;
  datePrecision?: string | null;
  includedFields: ShareContentField[];
  visibility?: ShareVisibility | null;
}

export type ShareVisibility = 'Public' | 'Unlisted';

export interface PassportProfileShareInput {
  selectedYears: number[];
  selectedParkIds: string[];
  selectedRatingKeys: string[];
  publicCaption?: string | null;
  visibility: ShareVisibility;
  allowsComparisons: boolean;
}

export interface PassportProfileShareYearCandidate {
  year: number;
  visitCount: number;
}

export interface PassportProfileShareParkCandidate {
  parkId: string;
  name: string;
  countryCode?: string | null;
  visitCount: number;
}

export interface PassportProfileShareRatingCandidate {
  selectionKey: string;
  name: string;
  parkName?: string | null;
  rating: number;
}

export interface PassportProfileShareSelection {
  years: PassportProfileShareYearCandidate[];
  parks: PassportProfileShareParkCandidate[];
  ratings: PassportProfileShareRatingCandidate[];
  savedSelectedYears?: number[] | null;
  savedSelectedParkIds?: string[] | null;
  savedSelectedRatingKeys?: string[] | null;
  savedPublicCaption?: string | null;
  savedVisibility: ShareVisibility;
  savedAllowsComparisons: boolean;
  hasSavedSnapshot: boolean;
}

export interface PassportProfileShareRatingSummary {
  ratedCount: number;
  eligibleCount: number;
  average?: number | null;
}

export interface PassportProfileShareCountry {
  countryCode: string;
  parkCount: number;
  visitCount: number;
}

export interface PassportProfileShareYear {
  year: number;
  visitCount: number;
  parkCount: number;
  completedRideCount?: number | null;
}

export interface PassportProfileSharePark {
  name: string;
  countryCode?: string | null;
  visitCount: number;
  firstVisitYear: number;
  lastVisitYear: number;
  completedRideCount?: number | null;
  visitRatings?: PassportProfileShareRatingSummary | null;
}

export interface PassportProfileShareRating {
  targetType: string;
  name: string;
  parkName?: string | null;
  category?: string | null;
  rating: number;
}

export interface PassportProfileShareMissedItem {
  name: string;
  status: string;
  occurrenceCount: number;
}

export interface PassportProfileSharePreview {
  displayName?: string | null;
  avatarUrl?: string | null;
  publicCaption?: string | null;
  visibility: ShareVisibility;
  allowsComparisons: boolean;
  parkCount?: number | null;
  visitCount?: number | null;
  totalRideCount?: number | null;
  distinctItemCount?: number | null;
  visitRatings?: PassportProfileShareRatingSummary | null;
  rideRatings?: PassportProfileShareRatingSummary | null;
  countries: PassportProfileShareCountry[];
  years: PassportProfileShareYear[];
  parks: PassportProfileSharePark[];
  personalRanking: PassportProfileShareRating[];
  missedItems: PassportProfileShareMissedItem[];
  hasIncompleteCatalog: boolean;
  calculationVersion: string;
  isEmpty: boolean;
}

export interface SharedPassportProfile {
  publishedAtUtc: string;
  passportProfile: PassportProfileSharePreview;
}

export interface VisitRecapShareInput {
  selectedParkItemIds?: string[] | null;
  publicCaption?: string | null;
}

export interface VisitRecapShareCandidates {
  items: VisitRecapShareItem[];
  totalEligibleItemCount: number;
  isTruncated: boolean;
  savedSelectedParkItemIds?: string[] | null;
  savedPublicCaption?: string | null;
  hasSavedSnapshot: boolean;
}

export interface VisitRecapShareDate {
  year: number;
  month?: number | null;
  day?: number | null;
  precision: string;
  isApproximate: boolean;
}

export interface VisitRecapShareItem {
  parkItemId: string;
  name: string;
  category?: string | null;
  rideCount?: number | null;
  averageRating?: number | null;
  isMissed: boolean;
}

export interface VisitRecapShareHighlight {
  name: string;
  rideCount?: number | null;
  rating?: number | null;
}

export interface VisitRecapSharePreview {
  parkId: string;
  parkName?: string | null;
  date?: VisitRecapShareDate | null;
  distinctItemCount?: number | null;
  totalRideCount?: number | null;
  categories: string[];
  parkRating?: number | null;
  topRatedItem?: VisitRecapShareHighlight | null;
  mostRepeatedItem?: VisitRecapShareHighlight | null;
  items: VisitRecapShareItem[];
  publicCaption?: string | null;
  hasHiddenDate: boolean;
  hasIncompleteRatings: boolean;
  hasIncompleteItems: boolean;
}

export interface SharedVisitRecapItem {
  name: string;
  category?: string | null;
  rideCount?: number | null;
  averageRating?: number | null;
  isMissed: boolean;
}

export interface SharedVisitRecapContent {
  parkId: string;
  parkName?: string | null;
  date?: VisitRecapShareDate | null;
  distinctItemCount?: number | null;
  totalRideCount?: number | null;
  categories: string[];
  parkRating?: number | null;
  topRatedItem?: VisitRecapShareHighlight | null;
  mostRepeatedItem?: VisitRecapShareHighlight | null;
  items: SharedVisitRecapItem[];
  publicCaption?: string | null;
  hasHiddenDate: boolean;
  hasIncompleteRatings: boolean;
  hasIncompleteItems: boolean;
}

export interface SharedVisitRecap {
  publishedAtUtc: string;
  visitRecap: SharedVisitRecapContent;
}

export interface YearRecapShareInput {
  publicCaption?: string | null;
}

export interface YearRecapShareSelection {
  savedPublicCaption?: string | null;
  hasSavedSnapshot: boolean;
}

export interface YearRecapShareRatingSummary {
  ratedCount: number;
  eligibleCount: number;
  average?: number | null;
}

export interface YearRecapSharePark {
  name: string;
  visitCount: number;
  completedRideCount?: number | null;
}

export interface YearRecapShareHighlight {
  name: string;
  rideCount: number;
  ratingCount: number;
  averageRating?: number | null;
  isNowClosed: boolean;
}

export interface YearRecapShareTrend {
  name: string;
  kind: string;
  firstWindowRatingCount: number;
  lastWindowRatingCount: number;
  firstWindowAverage: number;
  lastWindowAverage: number;
  delta: number;
}

export interface YearRecapSharePreview {
  year: number;
  parkCount?: number | null;
  visitCount: number;
  approximateVisitCount: number;
  approximateVisitRate: number;
  totalRideCount?: number | null;
  distinctItemCount?: number | null;
  missedItemCount?: number | null;
  categories: string[];
  parkRatings?: YearRecapShareRatingSummary | null;
  rideRatings?: YearRecapShareRatingSummary | null;
  mostVisitedParks: YearRecapSharePark[];
  mostRepeatedItem?: YearRecapShareHighlight | null;
  topRatedItem?: YearRecapShareHighlight | null;
  ratingEvolution?: YearRecapShareTrend | null;
  nowClosedItems: YearRecapShareHighlight[];
  publicCaption?: string | null;
  hasIncompleteCatalog: boolean;
  calculationVersion: string;
  isEmpty: boolean;
}

export interface SharedYearRecap {
  publishedAtUtc: string;
  yearRecap: YearRecapSharePreview;
}
