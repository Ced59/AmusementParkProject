import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';

export const SEARCH_API_ENDPOINTS = {
  getSearch: (query: string, categories: string[], page: number, size: number, region: ParkRegionFilter | null = null) => {
    const categoriesQuery: string = categories && categories.length > 0
      ? `&categories=${categories.join(',')}`
      : '';

    const regionQuery: string = region ? `&region=${encodeURIComponent(region)}` : '';
    return `search?query=${encodeURIComponent(query)}${categoriesQuery}&page=${page}&pageSize=${size}${regionQuery}`;
  }
};
