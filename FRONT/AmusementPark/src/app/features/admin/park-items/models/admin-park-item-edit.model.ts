import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { TranslationOption } from '@shared/utils/display/display-options';

export type AttractionLocationKey = 'entrance' | 'exit' | 'fastPassEntrance' | 'reducedMobilityEntrance';

export interface AttractionLocationOption {
  key: AttractionLocationKey;
  labelKey: string;
}

export interface ParkCoordinates {
  latitude: number;
  longitude: number;
}

export type SaveMode = 'stay' | 'back';
export type SaveScope = 'section' | 'all';

export type AdminParkItemCategoryOption = TranslationOption<ParkItemCategory>;
export type AdminParkItemTypeOption = TranslationOption<ParkItemType>;

export const ATTRACTION_LOCATION_OPTIONS: ReadonlyArray<AttractionLocationOption> = [
  { key: 'entrance', labelKey: 'admin.parks.items.locationFields.entrance' },
  { key: 'exit', labelKey: 'admin.parks.items.locationFields.exit' },
  { key: 'fastPassEntrance', labelKey: 'admin.parks.items.locationFields.fastPassEntrance' },
  { key: 'reducedMobilityEntrance', labelKey: 'admin.parks.items.locationFields.reducedMobilityEntrance' }
];

export interface AdminParkItemPhotoCategoryOption {
  slug: string;
  labelKey: string;
}

export const PARK_ITEM_PHOTO_CATEGORY_OPTIONS: ReadonlyArray<AdminParkItemPhotoCategoryOption> = [
  { slug: 'park-item-gallery', labelKey: 'admin.parks.items.photos.categories.gallery' },
  { slug: 'park-item-entrance', labelKey: 'admin.parks.items.photos.categories.entrance' },
  { slug: 'park-item-exit', labelKey: 'admin.parks.items.photos.categories.exit' },
  { slug: 'park-item-restriction-sign', labelKey: 'admin.parks.items.photos.categories.restrictionSign' },
  { slug: 'park-item-wait-time-sign', labelKey: 'admin.parks.items.photos.categories.waitTimeSign' },
  { slug: 'park-item-accessibility-sign', labelKey: 'admin.parks.items.photos.categories.accessibilitySign' },
  { slug: 'park-item-queue', labelKey: 'admin.parks.items.photos.categories.queue' },
  { slug: 'park-item-station', labelKey: 'admin.parks.items.photos.categories.station' },
  { slug: 'park-item-vehicle', labelKey: 'admin.parks.items.photos.categories.vehicle' },
  { slug: 'park-item-theming', labelKey: 'admin.parks.items.photos.categories.theming' },
  { slug: 'park-item-layout', labelKey: 'admin.parks.items.photos.categories.layout' },
  { slug: 'park-item-menu', labelKey: 'admin.parks.items.photos.categories.menu' },
  { slug: 'park-item-shop-front', labelKey: 'admin.parks.items.photos.categories.shopFront' },
  { slug: 'park-item-safety', labelKey: 'admin.parks.items.photos.categories.safety' }
];
