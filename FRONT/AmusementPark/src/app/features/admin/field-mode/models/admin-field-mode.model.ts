import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';

export type AdminFieldModeFilter = 'all' | 'missingPhotos' | 'missingGeneralLocation' | 'missingPreciseLocation';
export type AdminFieldModeLocationKey = 'general' | 'entrance' | 'exit' | 'fastPassEntrance' | 'reducedMobilityEntrance';
export type AdminFieldModeGpsStatus = 'idle' | 'checking' | 'ready' | 'error';

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

export interface AdminFieldModePhotoCategoryOption {
  slug: string;
  labelFr: string;
  labelEn: string;
}

export interface AdminFieldModeItemRow {
  item: ParkItem;
  photoCount: number | null;
  hasGeneralLocation: boolean;
  preciseLocationCount: number;
  hasAnyPreciseLocation: boolean;
}

export interface AdminFieldModeViewModel {
  selectedPark: Park | null;
  rows: AdminFieldModeItemRow[];
}

export const ADMIN_FIELD_MODE_SELECTED_PARK_STORAGE_KEY = 'admin.fieldMode.selectedParkId';
export const ADMIN_FIELD_MODE_GPS_MAX_AGE_MS = 2 * 60 * 1000;

export const ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS: ReadonlyArray<AdminFieldModePhotoCategoryOption> = [
  { slug: 'park-item-gallery', labelFr: 'Galerie générale', labelEn: 'General gallery' },
  { slug: 'park-item-entrance', labelFr: 'Entrée de l’attraction', labelEn: 'Attraction entrance' },
  { slug: 'park-item-exit', labelFr: 'Sortie', labelEn: 'Exit' },
  { slug: 'park-item-restriction-sign', labelFr: 'Panneau de restrictions', labelEn: 'Restriction sign' },
  { slug: 'park-item-wait-time-sign', labelFr: 'Panneau temps d’attente', labelEn: 'Wait-time sign' },
  { slug: 'park-item-accessibility-sign', labelFr: 'Accessibilité', labelEn: 'Accessibility' },
  { slug: 'park-item-queue', labelFr: 'File d’attente', labelEn: 'Queue' },
  { slug: 'park-item-station', labelFr: 'Gare / embarquement', labelEn: 'Station / boarding' },
  { slug: 'park-item-vehicle', labelFr: 'Véhicule / train', labelEn: 'Vehicle / train' },
  { slug: 'park-item-theming', labelFr: 'Décors / ambiance', labelEn: 'Theming' },
  { slug: 'park-item-layout', labelFr: 'Vue d’ensemble', labelEn: 'Overview' },
  { slug: 'park-item-menu', labelFr: 'Menu / tarifs', labelEn: 'Menu / prices' },
  { slug: 'park-item-shop-front', labelFr: 'Façade boutique / restaurant', labelEn: 'Shop / restaurant frontage' },
  { slug: 'park-item-safety', labelFr: 'Sécurité / consignes', labelEn: 'Safety instructions' }
];

export const ADMIN_FIELD_MODE_LOCATION_OPTIONS: ReadonlyArray<{ key: AdminFieldModeLocationKey; labelFr: string; labelEn: string }> = [
  { key: 'general', labelFr: 'Position générale', labelEn: 'General position' },
  { key: 'entrance', labelFr: 'Entrée précise', labelEn: 'Precise entrance' },
  { key: 'exit', labelFr: 'Sortie précise', labelEn: 'Precise exit' },
  { key: 'fastPassEntrance', labelFr: 'Accès rapide', labelEn: 'Fast pass entrance' },
  { key: 'reducedMobilityEntrance', labelFr: 'Accès PMR', labelEn: 'Reduced-mobility entrance' }
];
