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
