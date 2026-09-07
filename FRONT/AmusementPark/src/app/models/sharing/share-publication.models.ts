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
}

export interface SharePublicationPreviewRequest {
  publicationType: string;
  sourceId?: string | null;
  datePrecision: string;
  includedFields: ShareContentField[];
}

export interface SharePublicationPublishRequest {
  publicationType: string;
  sourceId?: string | null;
  approvedSourceVersion: number;
  approvedPolicySchemaVersion: number;
  approvedDatePrecision: string;
  approvedIncludedFields: ShareContentField[];
  approvalToken: string;
}

export interface SharePublicationSettings {
  isPublic: boolean;
  shareId?: string | null;
  publishedAtUtc?: string | null;
  policySchemaVersion?: number | null;
  datePrecision?: string | null;
  includedFields: ShareContentField[];
}
