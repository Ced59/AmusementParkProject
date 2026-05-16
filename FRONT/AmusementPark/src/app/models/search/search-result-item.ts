export interface SearchResultItem {
  originalId: string;
  resourceType?: string | null;
  category: string;
  title: string;
  subtitle?: string | null;
  description: string;
  city?: string | null;
  countryCode?: string | null;
  logoImageId?: string | null;
  attractionCount?: number | null;
  parentParkId?: string | null;
  parentParkName?: string | null;
}
