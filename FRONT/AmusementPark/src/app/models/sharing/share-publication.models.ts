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
}

export interface SharePublicationPreviewRequest {
  publicationType: string;
  sourceId?: string | null;
  datePrecision: string;
  includedFields: ShareContentField[];
  visitRecap?: VisitRecapShareInput | null;
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
}

export interface SharePublicationSettings {
  isPublic: boolean;
  shareId?: string | null;
  publishedAtUtc?: string | null;
  policySchemaVersion?: number | null;
  datePrecision?: string | null;
  includedFields: ShareContentField[];
}

export interface VisitRecapShareInput {
  selectedParkItemIds?: string[] | null;
  publicCaption?: string | null;
}

export interface VisitRecapShareCandidates {
  items: VisitRecapShareItem[];
  totalEligibleItemCount: number;
  isTruncated: boolean;
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
