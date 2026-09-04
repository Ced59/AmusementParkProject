export const PASSPORT_STATISTICS_API_ENDPOINTS = {
  item: (parkItemId: string): string => `me/passport/items/${encodeURIComponent(parkItemId)}/stats`,
  park: (parkId: string): string => `me/passport/parks/${encodeURIComponent(parkId)}/stats`,
  year: (year: number): string => `me/passport/years/${encodeURIComponent(String(year))}/stats`
} as const;
