import { PagedResult } from '@shared/models/contracts';

export type RatingTargetType = 'Park' | 'ParkItem';

export type RankingEvidenceLevel =
  | 'NoEvidence'
  | 'Insufficient'
  | 'Provisional'
  | 'Eligible'
  | 'Established'
  | 'StrongEvidence'
  | 'Excluded';

export type RankingIneligibilityReason =
  | 'NoRatings'
  | 'TooFewUniqueContributors'
  | 'TooFewComparableEntries'
  | 'InsufficientItemCoverage'
  | 'InsufficientCategoryCoverage'
  | 'TargetUnavailable'
  | 'TargetExcluded'
  | 'AggregateIntegrityFailure'
  | 'UnsupportedComposition';

export interface RankingEvidence {
  level: RankingEvidenceLevel;
  isEligibleForMainRanking: boolean;
  directParkContributorCount?: number | null;
  itemContributorCount?: number | null;
  eligibleItemCount?: number | null;
  eligibleCategoryCount?: number | null;
  ineligibilityReason?: RankingIneligibilityReason | null;
  nextThreshold?: number | null;
}

export interface RatingSummary {
  targetType: RatingTargetType;
  targetId: string;
  /** Compatibility alias for the retained observation count of this simple target. */
  ratingCount: number;
  ratingObservationCount?: number;
  uniqueContributorCount?: number | null;
  averageRating: number;
  bayesianScore: number;
  rank?: number | null;
  evidence?: RankingEvidence | null;
  methodologyVersion?: string | null;
}

export interface UserRatingUpsertRequest {
  targetType: RatingTargetType;
  targetId: string;
  value: number;
}

export interface UserRating {
  id: string;
  targetType: RatingTargetType;
  targetId: string;
  parkId: string;
  parkItemCategory?: string | null;
  parkItemType?: string | null;
  value: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  summary: RatingSummary;
}

export interface UserRatingListItem {
  id: string;
  targetType: RatingTargetType;
  targetId: string;
  targetName: string;
  parkId: string;
  parkName?: string | null;
  parkItemCategory?: string | null;
  parkItemType?: string | null;
  value: number;
  updatedAtUtc: string;
  summary: RatingSummary;
}

export interface UserRatingStatBucket {
  key: string;
  label: string;
  count: number;
  averageRating: number;
}

export interface UserRatingStats {
  totalRatings: number;
  averageRating: number;
  highestRating: number;
  lowestRating: number;
  byPark: UserRatingStatBucket[];
  byTargetType: UserRatingStatBucket[];
  byParkItemCategory: UserRatingStatBucket[];
}

export interface UserRankingShareSettings {
  isPublic: boolean;
  shareId?: string | null;
  publishedAtUtc?: string | null;
}

export interface UserRankingShareVisibilityRequest {
  isPublic: boolean;
}

export interface SharedUserRankingProfile {
  displayName: string;
  publishedAtUtc: string;
  isOwner: boolean;
  stats: UserRatingStats;
}

export interface ParkRatingRankingItem {
  targetId: string;
  targetName: string;
  parkItemCategory?: string | null;
  parkItemType?: string | null;
  ratingCount: number;
  averageRating: number;
  bayesianScore: number;
}

export interface ParkRatingRankingCategory {
  parkItemCategory: string;
  ratingCount: number;
  averageRating: number;
  bayesianScore: number;
  items: ParkRatingRankingItem[];
}

export interface ParkRatingRanking {
  rank: number;
  parkId: string;
  parkName: string;
  /** Compatibility alias for observations retained in the composed park score. */
  ratingCount: number;
  ratingObservationCount?: number;
  uniqueContributorCount?: number | null;
  score: number;
  parkRatingCount: number;
  parkAverageRating: number;
  itemsRatingCount: number;
  itemsAverageRating: number;
  evidence?: RankingEvidence | null;
  methodologyVersion?: string | null;
  categories: ParkRatingRankingCategory[];
}

export interface ParkItemRatingRanking {
  rank: number;
  targetId: string;
  targetName: string;
  parkId: string;
  parkName: string;
  parkItemCategory: string;
  parkItemType?: string | null;
  /** Compatibility alias for the retained observation count of this item. */
  ratingCount: number;
  ratingObservationCount?: number;
  uniqueContributorCount?: number | null;
  averageRating: number;
  bayesianScore: number;
  evidence?: RankingEvidence | null;
  methodologyVersion?: string | null;
}

export interface UserParkRatingRankingCategory {
  parkItemCategory: string;
  averageRating: number;
  items: UserRatingListItem[];
}

export interface UserParkRatingRanking {
  rank: number;
  parkId: string;
  parkName: string;
  ratingCount: number;
  averageRating: number;
  parkRating?: UserRatingListItem | null;
  categories: UserParkRatingRankingCategory[];
}

export interface UserParkItemRatingRanking {
  rank: number;
  rating: UserRatingListItem;
}

export type UserRatingsPage = PagedResult<UserRatingListItem>;
export type RatingRankingsPage = PagedResult<ParkRatingRanking>;
export type ParkItemRatingRankingsPage = PagedResult<ParkItemRatingRanking>;
export type UserParkRatingRankingsPage = PagedResult<UserParkRatingRanking>;
export type UserParkItemRatingRankingsPage = PagedResult<UserParkItemRatingRanking>;
