import { LocalizedItem } from '../shared/localized-item';
import { ParkItemCategory } from './park-item-category';
import { ParkItemType } from './park-item-type';

export interface ParkItem {
  id?: string;
  parkId: string;
  zoneId?: string | null;
  name: string;
  category: ParkItemCategory;
  type: ParkItemType;
  subtype?: string | null;
  latitude: number;
  longitude: number;
  descriptions?: LocalizedItem<string>[];
  isVisible?: boolean;
}
