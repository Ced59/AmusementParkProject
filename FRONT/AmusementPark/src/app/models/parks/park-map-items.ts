import { Park } from './park';

export interface ParkMapItems {
  park: Park;
  items: ParkMapItem[];
  zones: ParkMapZone[];
}

export interface ParkMapItem {
  id: string;
  name: string;
  category: string;
  type: string;
  subtype?: string | null;
  zoneId?: string | null;
  latitude: number;
  longitude: number;
}

export interface ParkMapZone {
  id: string;
  name: string;
  sortOrder: number;
}
