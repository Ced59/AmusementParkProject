import { PagedResult } from '@shared/models/contracts';

export type RatingTargetType = 'Park' | 'ParkItem';

export interface RatingSummary {
  targetType: RatingTargetType;
  targetId: string;
  ratingCount: number;
  averageRating: number;
  bayesianScore: number;
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

export interface RatingRankingItem {
  rank: number;
  targetType: RatingTargetType;
  targetId: string;
  targetName: string;
  parkId: string;
  parkName?: string | null;
  parkItemCategory?: string | null;
  parkItemType?: string | null;
  ratingCount: number;
  averageRating: number;
  bayesianScore: number;
}

export type UserRatingsPage = PagedResult<UserRatingListItem>;
export type RatingRankingsPage = PagedResult<RatingRankingItem>;
