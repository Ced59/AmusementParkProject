import { RatingTargetType } from '@app/models/ratings/rating.models';

export const RATINGS_API_ENDPOINTS = {
  getCurrentMethodology: 'ratings/methodology/current',
  getMethodology: (version: string) => `ratings/methodology/${encodeURIComponent(version)}`,
  getMethodologyHistory: 'ratings/methodology',
  getSummary: (targetType: RatingTargetType, targetId: string) =>
    `ratings/${encodeURIComponent(targetType)}/${encodeURIComponent(targetId)}/summary`,
  getMyRating: (targetType: RatingTargetType, targetId: string) =>
    `ratings/${encodeURIComponent(targetType)}/${encodeURIComponent(targetId)}/me`,
  deleteMyRating: (targetType: RatingTargetType, targetId: string) =>
    `ratings/${encodeURIComponent(targetType)}/${encodeURIComponent(targetId)}/me`,
  upsert: 'ratings',
  getMyRatings: (page: number, size: number, search: string | null = null) => {
    const params: string[] = [`page=${page}`, `size=${size}`];
    if (search) {
      params.push(`search=${encodeURIComponent(search)}`);
    }

    return `ratings/me?${params.join('&')}`;
  },
  getMyStats: 'ratings/me/stats',
  getMyShareSettings: 'ratings/me/share',
  setMyShareVisibility: 'ratings/me/share',
  getMyParkRankings: (
    page: number,
    size: number,
    search: string | null = null,
    targetId: string | null = null
  ) => {
    const params: string[] = [`page=${page}`, `size=${size}`];
    if (search) {
      params.push(`search=${encodeURIComponent(search)}`);
    }
    if (targetId) {
      params.push(`targetId=${encodeURIComponent(targetId)}`);
    }

    return `ratings/me/rankings/parks?${params.join('&')}`;
  },
  getMyParkItemRankings: (
    page: number,
    size: number,
    category: string,
    type: string | null = null,
    search: string | null = null,
    targetId: string | null = null
  ) => {
    const params: string[] = [
      `page=${page}`,
      `size=${size}`,
      `category=${encodeURIComponent(category)}`
    ];
    if (type) {
      params.push(`type=${encodeURIComponent(type)}`);
    }
    if (search) {
      params.push(`search=${encodeURIComponent(search)}`);
    }
    if (targetId) {
      params.push(`targetId=${encodeURIComponent(targetId)}`);
    }

    return `ratings/me/rankings/park-items?${params.join('&')}`;
  },
  getRankings: (page: number, size: number, category: string | null = null, search: string | null = null) => {
    const params: string[] = [`page=${page}`, `size=${size}`];
    if (category) {
      params.push(`category=${encodeURIComponent(category)}`);
    }
    if (search) {
      params.push(`search=${encodeURIComponent(search)}`);
    }

    return `ratings/rankings?${params.join('&')}`;
  },
  getParkItemRankings: (
    page: number,
    size: number,
    category: string,
    type: string | null = null,
    search: string | null = null
  ) => {
    const params: string[] = [
      `page=${page}`,
      `size=${size}`,
      `category=${encodeURIComponent(category)}`
    ];
    if (type) {
      params.push(`type=${encodeURIComponent(type)}`);
    }
    if (search) {
      params.push(`search=${encodeURIComponent(search)}`);
    }

    return `ratings/rankings/park-items?${params.join('&')}`;
  },
  getSharedProfile: (shareId: string) =>
    `ratings/shared/${encodeURIComponent(shareId)}`,
  getSharedParkRankings: (shareId: string, page: number, size: number, search: string | null = null) => {
    const params: string[] = [`page=${page}`, `size=${size}`];
    if (search) {
      params.push(`search=${encodeURIComponent(search)}`);
    }

    return `ratings/shared/${encodeURIComponent(shareId)}/parks?${params.join('&')}`;
  },
  getSharedParkItemRankings: (
    shareId: string,
    page: number,
    size: number,
    category: string,
    type: string | null = null,
    search: string | null = null
  ) => {
    const params: string[] = [
      `page=${page}`,
      `size=${size}`,
      `category=${encodeURIComponent(category)}`
    ];
    if (type) {
      params.push(`type=${encodeURIComponent(type)}`);
    }
    if (search) {
      params.push(`search=${encodeURIComponent(search)}`);
    }

    return `ratings/shared/${encodeURIComponent(shareId)}/park-items?${params.join('&')}`;
  },
  getSharedPreview: (shareId: string, category: string | null = null, type: string | null = null) => {
    const params: string[] = [];
    if (category) {
      params.push(`category=${encodeURIComponent(category)}`);
    }
    if (type) {
      params.push(`type=${encodeURIComponent(type)}`);
    }

    const query: string = params.length > 0 ? `?${params.join('&')}` : '';
    return `ratings/shared/${encodeURIComponent(shareId)}/preview.png${query}`;
  }
};
