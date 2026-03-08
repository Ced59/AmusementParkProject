import { LocalizedItem } from '../shared/localized-item';

export interface ParkZone {
  id?: string;
  parkId: string;
  name: string;
  slug?: string;
  descriptions?: LocalizedItem<string>[];
  isVisible?: boolean;
  sortOrder?: number;
}
