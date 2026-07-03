import { Park } from './park';
import { AttractionDetails } from './attraction-details';
import { LocalizedItem } from '../shared/localized-item';

export interface ParkMapItems {
  park: Park;
  items: ParkMapItem[];
  unlocatedItems?: ParkMapUnlocatedItem[];
  zones: ParkMapZone[];
}

export interface ParkMapItem {
  id: string;
  name: string;
  category: string;
  type: string;
  subtype?: string | null;
  zoneId?: string | null;
  descriptions?: LocalizedItem<string>[];
  attractionDetails?: AttractionDetails | null;
  latitude: number;
  longitude: number;
}

export interface ParkMapUnlocatedItem {
  id: string;
  name: string;
  category: string;
  type: string;
  subtype?: string | null;
  zoneId?: string | null;
  descriptions?: LocalizedItem<string>[];
  attractionDetails?: AttractionDetails | null;
}

export interface ParkMapZone {
  id: string;
  name: string;
  sortOrder: number;
}
