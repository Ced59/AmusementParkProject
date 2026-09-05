import { Park } from './park';
import { AttractionDetails } from './attraction-details';
import { LocalizedItem } from '../shared/localized-item';

export interface ParkMapItems {
  park: Park;
  items: ParkMapItem[];
  unlocatedItems?: ParkMapUnlocatedItem[];
  zones: ParkMapZone[];
  officialMaps?: ParkOfficialMap[];
}

export type ParkOfficialMapFormat = 'Image' | 'Pdf' | 'Other';

export interface ParkOfficialMap {
  id: string;
  year: number;
  format: ParkOfficialMapFormat;
  documentUrl: string;
  isVisible?: boolean;
  originalFileName?: string | null;
  contentType?: string | null;
  sizeInBytes?: number | null;
  previewImageUrl?: string | null;
  sourcePageUrl?: string | null;
  languageCode?: string | null;
  titles?: LocalizedItem<string>[];
  alternativeTexts?: LocalizedItem<string>[];
  lastVerifiedAtUtc?: string | null;
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
