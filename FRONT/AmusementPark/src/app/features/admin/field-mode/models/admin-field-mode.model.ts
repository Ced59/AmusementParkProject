import { PARK_ITEM_PHOTO_CATEGORIES, ParkItemPhotoCategoryDefinition } from '@app/models/images/park-item-photo-category';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';

export type AdminFieldModeFilter = 'all' | 'missingPhotos' | 'missingGeneralLocation' | 'missingPreciseLocation';
export type AdminFieldModeProcessedFilter = 'unprocessed' | 'processed' | 'all';
export type AdminFieldModeLocationKey = 'general' | 'entrance' | 'exit' | 'fastPassEntrance' | 'reducedMobilityEntrance';
export type AdminFieldModeGpsStatus = 'idle' | 'checking' | 'ready' | 'error';
export type AdminFieldModePhotoInspectionStatus = 'accepted' | 'invalid' | 'missingGps';

export interface AdminFieldModeParkOption {
  label: string;
  value: string;
}

export interface AdminFieldModePosition {
  latitude: number;
  longitude: number;
  accuracy: number | null;
  capturedAt: number;
}

export type AdminFieldModePhotoCategoryOption = Pick<ParkItemPhotoCategoryDefinition, 'slug' | 'labelFr' | 'labelEn'>;

export interface AdminFieldModePhotoInspection {
  id: string;
  fileName: string;
  sizeInBytes: number;
  contentType: string | null;
  lastModified: number | null;
  width: number | null;
  height: number | null;
  gpsDetected: boolean;
  latitude: number | null;
  longitude: number | null;
  status: AdminFieldModePhotoInspectionStatus;
  previewUrl: string | null;
}

export interface AdminFieldModePhotoSelection {
  id: string;
  file: File;
  position: AdminFieldModePosition;
  previewUrl: string;
}

export interface AdminFieldModeItemRow {
  item: ParkItem;
  photoCount: number | null;
  hasGeneralLocation: boolean;
  preciseLocationCount: number;
  hasAnyPreciseLocation: boolean;
  isProcessed: boolean;
}

export interface AdminFieldModeViewModel {
  selectedPark: Park | null;
  rows: AdminFieldModeItemRow[];
}

export const ADMIN_FIELD_MODE_SELECTED_PARK_STORAGE_KEY = 'admin.fieldMode.selectedParkId';
export const ADMIN_FIELD_MODE_GPS_MAX_AGE_MS = 2 * 60 * 1000;
export const ADMIN_FIELD_MODE_GPS_TARGET_ACCURACY_METERS = 6;

export const ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS: ReadonlyArray<AdminFieldModePhotoCategoryOption> = PARK_ITEM_PHOTO_CATEGORIES;

export const ADMIN_FIELD_MODE_LOCATION_OPTIONS: ReadonlyArray<{ key: AdminFieldModeLocationKey; labelFr: string; labelEn: string }> = [
  { key: 'general', labelFr: 'Position générale', labelEn: 'General position' },
  { key: 'entrance', labelFr: 'Entrée précise', labelEn: 'Precise entrance' },
  { key: 'exit', labelFr: 'Sortie précise', labelEn: 'Exit' },
  { key: 'fastPassEntrance', labelFr: 'Accès rapide', labelEn: 'Fast pass entrance' },
  { key: 'reducedMobilityEntrance', labelFr: 'Accès PMR', labelEn: 'Reduced-mobility entrance' }
];