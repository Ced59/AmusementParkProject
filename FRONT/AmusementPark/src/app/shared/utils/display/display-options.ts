import { AttractionAccessConditionType } from '@app/models/parks/attraction-access-condition-type';
import { AttractionAccessConditionUnit } from '@app/models/parks/attraction-access-condition-unit';
import { AttractionWaterExposureLevel } from '@app/models/parks/attraction-water-exposure-level';
import { AttractionStatus } from '@app/models/parks/attraction-status';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParkType } from '@app/models/parks/park-type';
import {
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  getParkTypeTranslationKey
} from './display-label.helpers';

export interface TranslationOption<TValue> {
  labelKey: string;
  value: TValue;
}

export const PARK_TYPE_OPTIONS: ReadonlyArray<TranslationOption<ParkType>> = [
  buildTranslationOption('ThemePark', getParkTypeTranslationKey),
  buildTranslationOption('WaterPark', getParkTypeTranslationKey),
  buildTranslationOption('Zoo', getParkTypeTranslationKey),
  buildTranslationOption('AnimalPark', getParkTypeTranslationKey),
  buildTranslationOption('AmusementPark', getParkTypeTranslationKey),
  buildTranslationOption('Resort', getParkTypeTranslationKey)
];

export const PARK_ITEM_CATEGORY_OPTIONS: ReadonlyArray<TranslationOption<ParkItemCategory>> = [
  buildTranslationOption('Attraction', getParkItemCategoryTranslationKey),
  buildTranslationOption('Restaurant', getParkItemCategoryTranslationKey),
  buildTranslationOption('Hotel', getParkItemCategoryTranslationKey),
  buildTranslationOption('Animal', getParkItemCategoryTranslationKey),
  buildTranslationOption('Show', getParkItemCategoryTranslationKey),
  buildTranslationOption('Shop', getParkItemCategoryTranslationKey),
  buildTranslationOption('Service', getParkItemCategoryTranslationKey),
  buildTranslationOption('Transport', getParkItemCategoryTranslationKey),
  buildTranslationOption('Other', getParkItemCategoryTranslationKey)
];

export const PARK_ITEM_TYPE_OPTIONS: ReadonlyArray<TranslationOption<ParkItemType>> = [
  buildTranslationOption('Attraction', getParkItemTypeTranslationKey),
  buildTranslationOption('RollerCoaster', getParkItemTypeTranslationKey),
  buildTranslationOption('WaterRide', getParkItemTypeTranslationKey),
  buildTranslationOption('FlatRide', getParkItemTypeTranslationKey),
  buildTranslationOption('DarkRide', getParkItemTypeTranslationKey),
  buildTranslationOption('FamilyRide', getParkItemTypeTranslationKey),
  buildTranslationOption('ThrillRide', getParkItemTypeTranslationKey),
  buildTranslationOption('TransportRide', getParkItemTypeTranslationKey),
  buildTranslationOption('WalkThrough', getParkItemTypeTranslationKey),
  buildTranslationOption('Playground', getParkItemTypeTranslationKey),
  buildTranslationOption('InteractiveExperience', getParkItemTypeTranslationKey),
  buildTranslationOption('Game', getParkItemTypeTranslationKey),
  buildTranslationOption('MeetAndGreet', getParkItemTypeTranslationKey),
  buildTranslationOption('ObservationRide', getParkItemTypeTranslationKey),
  buildTranslationOption('AnimalExhibit', getParkItemTypeTranslationKey),
  buildTranslationOption('Restaurant', getParkItemTypeTranslationKey),
  buildTranslationOption('Snack', getParkItemTypeTranslationKey),
  buildTranslationOption('Hotel', getParkItemTypeTranslationKey),
  buildTranslationOption('Show', getParkItemTypeTranslationKey),
  buildTranslationOption('Shop', getParkItemTypeTranslationKey),
  buildTranslationOption('Service', getParkItemTypeTranslationKey),
  buildTranslationOption('Toilets', getParkItemTypeTranslationKey),
  buildTranslationOption('FirstAid', getParkItemTypeTranslationKey),
  buildTranslationOption('Information', getParkItemTypeTranslationKey),
  buildTranslationOption('Locker', getParkItemTypeTranslationKey),
  buildTranslationOption('Parking', getParkItemTypeTranslationKey),
  buildTranslationOption('Transport', getParkItemTypeTranslationKey),
  buildTranslationOption('Station', getParkItemTypeTranslationKey),
  buildTranslationOption('Other', getParkItemTypeTranslationKey)
];

export const ATTRACTION_TYPE_OPTIONS: ReadonlyArray<TranslationOption<ParkItemType>> = [
  buildTranslationOption('Attraction', getParkItemTypeTranslationKey),
  buildTranslationOption('RollerCoaster', getParkItemTypeTranslationKey),
  buildTranslationOption('WaterRide', getParkItemTypeTranslationKey),
  buildTranslationOption('FlatRide', getParkItemTypeTranslationKey),
  buildTranslationOption('DarkRide', getParkItemTypeTranslationKey),
  buildTranslationOption('FamilyRide', getParkItemTypeTranslationKey),
  buildTranslationOption('ThrillRide', getParkItemTypeTranslationKey),
  buildTranslationOption('TransportRide', getParkItemTypeTranslationKey),
  buildTranslationOption('WalkThrough', getParkItemTypeTranslationKey),
  buildTranslationOption('Playground', getParkItemTypeTranslationKey),
  buildTranslationOption('InteractiveExperience', getParkItemTypeTranslationKey),
  buildTranslationOption('Game', getParkItemTypeTranslationKey),
  buildTranslationOption('MeetAndGreet', getParkItemTypeTranslationKey),
  buildTranslationOption('ObservationRide', getParkItemTypeTranslationKey),
  buildTranslationOption('Other', getParkItemTypeTranslationKey)
];

export const NON_ATTRACTION_TYPE_OPTIONS_BY_CATEGORY: Readonly<Record<Exclude<ParkItemCategory, 'Attraction'>, ReadonlyArray<TranslationOption<ParkItemType>>>> = {
  Restaurant: [
    buildTranslationOption('Restaurant', getParkItemTypeTranslationKey),
    buildTranslationOption('Snack', getParkItemTypeTranslationKey)
  ],
  Hotel: [
    buildTranslationOption('Hotel', getParkItemTypeTranslationKey)
  ],
  Animal: [
    buildTranslationOption('AnimalExhibit', getParkItemTypeTranslationKey)
  ],
  Show: [
    buildTranslationOption('Show', getParkItemTypeTranslationKey)
  ],
  Shop: [
    buildTranslationOption('Shop', getParkItemTypeTranslationKey)
  ],
  Service: [
    buildTranslationOption('Service', getParkItemTypeTranslationKey),
    buildTranslationOption('Toilets', getParkItemTypeTranslationKey),
    buildTranslationOption('FirstAid', getParkItemTypeTranslationKey),
    buildTranslationOption('Information', getParkItemTypeTranslationKey),
    buildTranslationOption('Locker', getParkItemTypeTranslationKey),
    buildTranslationOption('Parking', getParkItemTypeTranslationKey)
  ],
  Transport: [
    buildTranslationOption('Transport', getParkItemTypeTranslationKey),
    buildTranslationOption('Station', getParkItemTypeTranslationKey)
  ],
  Other: [
    buildTranslationOption('Other', getParkItemTypeTranslationKey)
  ]
};

export const ATTRACTION_ACCESS_CONDITION_PRESET_OPTIONS: ReadonlyArray<TranslationOption<AttractionAccessConditionType>> = [
  { labelKey: 'admin.parks.items.accessConditionTypes.minHeight', value: 'MinHeight' },
  { labelKey: 'admin.parks.items.accessConditionTypes.minHeightAccompanied', value: 'MinHeightAccompanied' },
  { labelKey: 'admin.parks.items.accessConditionTypes.maxHeight', value: 'MaxHeight' },
  { labelKey: 'admin.parks.items.accessConditionTypes.minAge', value: 'MinAge' },
  { labelKey: 'admin.parks.items.accessConditionTypes.minAgeAccompanied', value: 'MinAgeAccompanied' },
  { labelKey: 'admin.parks.items.accessConditionTypes.pregnancyRestriction', value: 'PregnancyRestriction' },
  { labelKey: 'admin.parks.items.accessConditionTypes.heartRestriction', value: 'HeartRestriction' },
  { labelKey: 'admin.parks.items.accessConditionTypes.backNeckRestriction', value: 'BackNeckRestriction' },
  { labelKey: 'admin.parks.items.accessConditionTypes.wheelchairTransferRequired', value: 'WheelchairTransferRequired' },
  { labelKey: 'admin.parks.items.accessConditionTypes.accessPassRequired', value: 'AccessPassRequired' },
  { labelKey: 'admin.parks.items.accessConditionTypes.custom', value: 'Custom' }
];


export const ATTRACTION_STATUS_OPTIONS: ReadonlyArray<TranslationOption<AttractionStatus>> = [
  { labelKey: 'admin.parks.items.statuses.operating', value: 'Operating' },
  { labelKey: 'admin.parks.items.statuses.underConstruction', value: 'UnderConstruction' },
  { labelKey: 'admin.parks.items.statuses.temporarilyClosed', value: 'TemporarilyClosed' },
  { labelKey: 'admin.parks.items.statuses.closedDefinitively', value: 'ClosedDefinitively' },
  { labelKey: 'admin.parks.items.statuses.removed', value: 'Removed' },
  { labelKey: 'admin.parks.items.statuses.planned', value: 'Planned' },
  { labelKey: 'admin.parks.items.statuses.unknown', value: 'Unknown' }
];

export const ATTRACTION_WATER_EXPOSURE_LEVEL_OPTIONS: ReadonlyArray<TranslationOption<AttractionWaterExposureLevel>> = [
  { labelKey: 'admin.parks.items.waterExposureLevels.none', value: 'None' },
  { labelKey: 'admin.parks.items.waterExposureLevels.splash', value: 'Splash' },
  { labelKey: 'admin.parks.items.waterExposureLevels.moderate', value: 'Moderate' },
  { labelKey: 'admin.parks.items.waterExposureLevels.soaking', value: 'Soaking' },
  { labelKey: 'admin.parks.items.waterExposureLevels.extremeSoaking', value: 'ExtremeSoaking' }
];

export const ATTRACTION_ACCESS_CONDITION_UNIT_OPTIONS: ReadonlyArray<TranslationOption<AttractionAccessConditionUnit>> = [
  { labelKey: 'admin.parks.items.accessConditionUnits.centimeter', value: 'Centimeter' },
  { labelKey: 'admin.parks.items.accessConditionUnits.year', value: 'Year' }
];

export function buildTranslationOptions<TValue extends string>(
  values: readonly TValue[],
  labelKeyResolver: (value: TValue) => string
): Array<TranslationOption<TValue>> {
  return values.map((value: TValue) => buildTranslationOption(value, labelKeyResolver));
}

function buildTranslationOption<TValue extends string>(
  value: TValue,
  labelKeyResolver: (value: TValue) => string
): TranslationOption<TValue> {
  return {
    labelKey: labelKeyResolver(value),
    value
  };
}
