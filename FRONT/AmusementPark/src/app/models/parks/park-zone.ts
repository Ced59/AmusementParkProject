import { LocalizedItem } from '../shared/localized-item';

export interface ParkZone {
  id?: string;
  parkId: string;
  name?: string;
  names?: LocalizedItem<string>[];
  slug?: string;
  descriptions?: LocalizedItem<string>[];
  latitude?: number | null;
  longitude?: number | null;
  isVisible?: boolean;
  sortOrder?: number;
}
