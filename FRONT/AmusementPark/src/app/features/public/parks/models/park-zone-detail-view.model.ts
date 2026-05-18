import { ParkDetailInfoRowViewModel } from './park-detail-info-row.model';

export interface ParkZoneDetailViewModel {
  id: string | null;
  name: string;
  slug: string | null;
  description: string | null;
  totalItems: number;
  topCounts: ParkZoneDetailCountViewModel[];
  latitude: number | null;
  longitude: number | null;
  isVisible: boolean;
  sortOrder: number | null;
  itemsLink: string[] | null;
  queryParams: Record<string, string> | null;
  infoRows: ParkDetailInfoRowViewModel[];
}

export interface ParkZoneDetailCountViewModel {
  labelKey: string;
  count: number;
  icon: string;
}
