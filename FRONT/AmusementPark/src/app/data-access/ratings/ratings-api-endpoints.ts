import { RatingTargetType } from '@app/models/ratings/rating.models';

export const RATINGS_API_ENDPOINTS = {
  getSummary: (targetType: RatingTargetType, targetId: string) =>
    `ratings/${encodeURIComponent(targetType)}/${encodeURIComponent(targetId)}/summary`,
  getMyRating: (targetType: RatingTargetType, targetId: string) =>
    `ratings/${encodeURIComponent(targetType)}/${encodeURIComponent(targetId)}/me`,
  upsert: 'ratings',
  getMyRatings: (page: number, size: number) => `ratings/me?page=${page}&size=${size}`,
  getMyStats: 'ratings/me/stats',
  getRankings: (page: number, size: number, targetType: RatingTargetType | null = null, category: string | null = null) => {
    const params: string[] = [`page=${page}`, `size=${size}`];
    if (targetType) {
      params.push(`targetType=${encodeURIComponent(targetType)}`);
    }
    if (category) {
      params.push(`category=${encodeURIComponent(category)}`);
    }

    return `ratings/rankings?${params.join('&')}`;
  }
};
