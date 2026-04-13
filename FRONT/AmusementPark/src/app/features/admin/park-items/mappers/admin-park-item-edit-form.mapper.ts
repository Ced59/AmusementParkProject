import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';

import { AttractionAccessCondition } from '@app/models/parks/attraction-access-condition';
import { AttractionAccessConditionType } from '@app/models/parks/attraction-access-condition-type';
import { AttractionAccessConditionUnit } from '@app/models/parks/attraction-access-condition-unit';
import { AttractionDetails } from '@app/models/parks/attraction-details';
import { AttractionLocationPoint } from '@app/models/parks/attraction-location-point';
import { AttractionLocations } from '@app/models/parks/attraction-locations';
import { AttractionWaterExposureLevel } from '@app/models/parks/attraction-water-exposure-level';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { ParkItemType } from '@app/models/parks/park-item-type';
import {
  createAdminParkItemAccessConditionGroup,
  hasLocalizedValues,
  setAdminParkItemAccessConditions,
  toLocalizedItems
} from './admin-park-item-access-condition-form.utils';

export function createAdminParkItemEditForm(formBuilder: FormBuilder, parkId: string): FormGroup {
  return formBuilder.group({
    parkId: [parkId, Validators.required],
    zoneId: [null],
    name: ['', Validators.required],
    category: ['Attraction', Validators.required],
    type: ['Attraction', Validators.required],
    subtype: [''],
    latitude: [48.8566, Validators.required],
    longitude: [2.3522, Validators.required],
    descriptions: [[]],
    attractionDetails: formBuilder.group({
      manufacturerId: [null],
      model: [''],
      openingDate: [''],
      closingDate: [''],
      durationInSeconds: [null],
      capacityPerHour: [null],
      heightInMeters: [null],
      lengthInMeters: [null],
      speedInKmH: [null],
      dropInMeters: [null],
      inversionCount: [null],
      trainCount: [null],
      carsPerTrain: [null],
      ridersPerVehicle: [null],
      hasSingleRider: [false],
      hasFastPass: [false],
      isAccessibleForReducedMobility: [false],
      isIndoor: [false],
      waterExposureLevel: [null],
      accessConditions: formBuilder.array([])
    }),
    attractionLocations: formBuilder.group({
      entrance: createLocationGroup(formBuilder),
      exit: createLocationGroup(formBuilder),
      fastPassEntrance: createLocationGroup(formBuilder),
      reducedMobilityEntrance: createLocationGroup(formBuilder)
    }),
    isVisible: [true]
  });
}

export function patchAdminParkItemEditForm(
  formBuilder: FormBuilder,
  form: FormGroup,
  item: ParkItem
): void {
  form.patchValue({
    parkId: item.parkId,
    zoneId: item.zoneId ?? null,
    name: item.name,
    category: item.category,
    type: item.type,
    subtype: item.subtype ?? '',
    latitude: item.latitude,
    longitude: item.longitude,
    descriptions: item.descriptions ?? [],
    isVisible: item.isVisible ?? true
  }, { emitEvent: false });

  patchAttractionDetails(formBuilder, form, item.attractionDetails ?? null);
  patchAttractionLocations(form, item.attractionLocations ?? null);
}

export function buildAdminParkItemEditSnapshot(form: FormGroup): string {
  return JSON.stringify(mapAdminParkItemEditFormToParkItem(form));
}

export function getAdminParkItemFirstInvalidTabIndex(form: FormGroup): number {
  if (hasInvalidGeneralTab(form)) {
    return 0;
  }

  const category: ParkItemCategory = form.get('category')?.value as ParkItemCategory;

  if (category !== 'Attraction') {
    return 0;
  }

  if ((form.get('attractionDetails') as FormGroup).invalid) {
    return 1;
  }

  const accessConditions: FormArray = getAccessConditions(form);
  if (accessConditions.invalid) {
    return 2;
  }

  if ((form.get('attractionLocations') as FormGroup).invalid) {
    return 3;
  }

  return 0;
}

export function mapAdminParkItemEditFormToParkItem(form: FormGroup): ParkItem {
  const raw: AdminParkItemFormValue = form.getRawValue() as AdminParkItemFormValue;
  const category: ParkItemCategory = raw.category as ParkItemCategory;

  return {
    parkId: raw.parkId,
    zoneId: raw.zoneId || null,
    name: raw.name,
    category,
    type: toParkItemType(raw.type, category),
    subtype: raw.subtype || null,
    latitude: toRequiredNumber(raw.latitude),
    longitude: toRequiredNumber(raw.longitude),
    descriptions: raw.descriptions ?? [],
    attractionDetails: category === 'Attraction' ? buildAttractionDetails(raw.attractionDetails) : null,
    attractionLocations: category === 'Attraction' ? buildAttractionLocations(raw.attractionLocations) : null,
    isVisible: !!raw.isVisible
  };
}

function hasInvalidGeneralTab(form: FormGroup): boolean {
  return !!form.get('parkId')?.invalid
    || !!form.get('name')?.invalid
    || !!form.get('category')?.invalid
    || !!form.get('type')?.invalid
    || !!form.get('latitude')?.invalid
    || !!form.get('longitude')?.invalid;
}

function patchAttractionDetails(formBuilder: FormBuilder, form: FormGroup, details: AttractionDetails | null): void {
  form.get('attractionDetails')?.patchValue({
    manufacturerId: details?.manufacturerId ?? null,
    model: details?.model ?? '',
    openingDate: normalizeDateForInput(details?.openingDate),
    closingDate: normalizeDateForInput(details?.closingDate),
    durationInSeconds: details?.durationInSeconds ?? null,
    capacityPerHour: details?.capacityPerHour ?? null,
    heightInMeters: details?.heightInMeters ?? null,
    lengthInMeters: details?.lengthInMeters ?? null,
    speedInKmH: details?.speedInKmH ?? null,
    dropInMeters: details?.dropInMeters ?? null,
    inversionCount: details?.inversionCount ?? null,
    trainCount: details?.trainCount ?? null,
    carsPerTrain: details?.carsPerTrain ?? null,
    ridersPerVehicle: details?.ridersPerVehicle ?? null,
    hasSingleRider: details?.hasSingleRider ?? false,
    hasFastPass: details?.hasFastPass ?? false,
    isAccessibleForReducedMobility: details?.isAccessibleForReducedMobility ?? false,
    isIndoor: details?.isIndoor ?? false,
    waterExposureLevel: details?.waterExposureLevel ?? null
  }, { emitEvent: false });

  setAdminParkItemAccessConditions(formBuilder, getAccessConditions(form), details?.accessConditions ?? null);
}

function patchAttractionLocations(form: FormGroup, locations: AttractionLocations | null): void {
  patchLocation(form, 'entrance', locations?.entrance ?? null);
  patchLocation(form, 'exit', locations?.exit ?? null);
  patchLocation(form, 'fastPassEntrance', locations?.fastPassEntrance ?? null);
  patchLocation(form, 'reducedMobilityEntrance', locations?.reducedMobilityEntrance ?? null);
}

function patchLocation(form: FormGroup, controlName: string, point: AttractionLocationPoint | null): void {
  form.get(['attractionLocations', controlName])?.patchValue({
    latitude: point?.latitude ?? null,
    longitude: point?.longitude ?? null
  }, { emitEvent: false });
}

function buildAttractionDetails(raw: AdminAttractionDetailsFormValue | null | undefined): AttractionDetails | null {
  const details: AttractionDetails = {
    manufacturerId: toNullableText(raw?.manufacturerId),
    model: toNullableText(raw?.model),
    openingDate: toNullableDateText(raw?.openingDate),
    closingDate: toNullableDateText(raw?.closingDate),
    durationInSeconds: toNullableInteger(raw?.durationInSeconds),
    capacityPerHour: toNullableInteger(raw?.capacityPerHour),
    heightInMeters: toNullableNumber(raw?.heightInMeters),
    lengthInMeters: toNullableNumber(raw?.lengthInMeters),
    speedInKmH: toNullableNumber(raw?.speedInKmH),
    dropInMeters: toNullableNumber(raw?.dropInMeters),
    inversionCount: toNullableInteger(raw?.inversionCount),
    trainCount: toNullableInteger(raw?.trainCount),
    carsPerTrain: toNullableInteger(raw?.carsPerTrain),
    ridersPerVehicle: toNullableInteger(raw?.ridersPerVehicle),
    hasSingleRider: raw?.hasSingleRider ?? false,
    hasFastPass: raw?.hasFastPass ?? false,
    isAccessibleForReducedMobility: raw?.isAccessibleForReducedMobility ?? false,
    isIndoor: raw?.isIndoor ?? false,
    waterExposureLevel: toNullableWaterExposureLevel(raw?.waterExposureLevel),
    accessConditions: buildAttractionAccessConditions(raw?.accessConditions)
  };

  return hasAtLeastOneAttractionDetail(details) ? details : null;
}

function buildAttractionAccessConditions(
  raw: Array<AdminAttractionAccessConditionFormValue> | null | undefined
): AttractionAccessCondition[] | null {
  if (!raw || raw.length === 0) {
    return null;
  }

  const conditions: AttractionAccessCondition[] = raw
    .map((item: AdminAttractionAccessConditionFormValue, index: number) => buildAttractionAccessCondition(item, index))
    .filter((item: AttractionAccessCondition | null): item is AttractionAccessCondition => item !== null);

  return conditions.length > 0 ? conditions : null;
}

function buildAttractionAccessCondition(
  raw: AdminAttractionAccessConditionFormValue | null | undefined,
  index: number
): AttractionAccessCondition | null {
  const type: AttractionAccessConditionType = (raw?.type as AttractionAccessConditionType) ?? 'Custom';
  const label: LocalizedItem<string>[] | null = toLocalizedItems(raw?.label);
  const description: LocalizedItem<string>[] | null = toLocalizedItems(raw?.description);
  const condition: AttractionAccessCondition = {
    type,
    isCustom: type === 'Custom',
    value: toNullableNumber(raw?.value),
    unit: toNullableUnit(raw?.unit),
    requiresAccompaniment: !!raw?.requiresAccompaniment,
    minimumCompanionAge: toNullableInteger(raw?.minimumCompanionAge),
    label,
    description,
    displayOrder: index + 1
  };

  if (!hasAtLeastOneAccessConditionValue(condition)) {
    return null;
  }

  return condition;
}

function buildAttractionLocations(raw: AdminAttractionLocationsFormValue | null | undefined): AttractionLocations | null {
  const locations: AttractionLocations = {
    entrance: buildLocationPoint(raw?.entrance),
    exit: buildLocationPoint(raw?.exit),
    fastPassEntrance: buildLocationPoint(raw?.fastPassEntrance),
    reducedMobilityEntrance: buildLocationPoint(raw?.reducedMobilityEntrance)
  };

  if (!locations.entrance && !locations.exit && !locations.fastPassEntrance && !locations.reducedMobilityEntrance) {
    return null;
  }

  return locations;
}

function buildLocationPoint(raw: AdminAttractionLocationPointFormValue | null | undefined): AttractionLocationPoint | null {
  const latitude: number | null = toNullableNumber(raw?.latitude);
  const longitude: number | null = toNullableNumber(raw?.longitude);

  if (latitude === null || longitude === null) {
    return null;
  }

  return {
    latitude,
    longitude
  };
}

function hasAtLeastOneAttractionDetail(details: AttractionDetails): boolean {
  return Object.values(details).some((value: string | number | boolean | AttractionAccessCondition[] | null | undefined) => {
    if (typeof value === 'boolean') {
      return value === true;
    }

    if (Array.isArray(value)) {
      return value.length > 0;
    }

    return value !== null && value !== undefined && value !== '';
  });
}

function hasAtLeastOneAccessConditionValue(condition: AttractionAccessCondition): boolean {
  if (condition.type !== 'Custom') {
    return true;
  }

  return condition.value !== null
    || condition.unit !== null
    || condition.requiresAccompaniment === true
    || condition.minimumCompanionAge !== null
    || hasLocalizedValues(condition.label)
    || hasLocalizedValues(condition.description);
}

function createLocationGroup(formBuilder: FormBuilder): FormGroup {
  return formBuilder.group({
    latitude: [null],
    longitude: [null]
  });
}

function getAccessConditions(form: FormGroup): FormArray {
  return form.get(['attractionDetails', 'accessConditions']) as FormArray;
}

function normalizeDateForInput(value: unknown): string {
  const normalized: string = String(value ?? '').trim();

  if (normalized.length === 0) {
    return '';
  }

  const isoDateMatch: RegExpMatchArray | null = normalized.match(/^(\d{4}-\d{2}-\d{2})/);
  if (isoDateMatch) {
    return isoDateMatch[1];
  }

  const parsedDate: Date = new Date(normalized);
  if (Number.isNaN(parsedDate.getTime())) {
    return '';
  }

  const year: string = String(parsedDate.getUTCFullYear()).padStart(4, '0');
  const month: string = String(parsedDate.getUTCMonth() + 1).padStart(2, '0');
  const day: string = String(parsedDate.getUTCDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

function toNullableText(value: unknown): string | null {
  const normalized: string = String(value ?? '').trim();
  return normalized.length > 0 ? normalized : null;
}

function toNullableDateText(value: unknown): string | null {
  const normalized: string = normalizeDateForInput(value);
  return normalized.length > 0 ? normalized : null;
}

function toNullableInteger(value: unknown): number | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const parsed: number = Number(value);
  return Number.isFinite(parsed) ? Math.trunc(parsed) : null;
}

function toNullableNumber(value: unknown): number | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const parsed: number = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function toNullableUnit(value: unknown): AttractionAccessConditionUnit | null {
  return value === 'Centimeter' || value === 'Year'
    ? value
    : null;
}

function toNullableWaterExposureLevel(value: unknown): AttractionWaterExposureLevel | null {
  return value === 'None' || value === 'Splash' || value === 'Moderate' || value === 'Soaking' || value === 'ExtremeSoaking'
    ? value
    : null;
}

function toRequiredNumber(value: unknown): number {
  const parsed: number = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function toParkItemType(value: unknown, category: ParkItemCategory): ParkItemType {
  const normalized: string = String(value ?? '').trim();
  const allowedTypes: ReadonlyArray<ParkItemType> = category === 'Attraction'
    ? [
      'Attraction',
      'RollerCoaster',
      'WaterRide',
      'FlatRide',
      'DarkRide',
      'FamilyRide',
      'ThrillRide',
      'TransportRide',
      'WalkThrough',
      'Playground',
      'InteractiveExperience',
      'ObservationRide',
      'Other'
    ]
    : category === 'Restaurant'
      ? ['Restaurant', 'Snack']
      : category === 'Hotel'
        ? ['Hotel']
        : category === 'Animal'
          ? ['AnimalExhibit']
          : category === 'Show'
            ? ['Show']
            : category === 'Shop'
              ? ['Shop']
              : category === 'Service'
                ? ['Service']
                : category === 'Transport'
                  ? ['Transport']
                  : ['Other'];

  return allowedTypes.includes(normalized as ParkItemType)
    ? normalized as ParkItemType
    : allowedTypes[0];
}

interface AdminAttractionLocationPointFormValue {
  latitude: unknown;
  longitude: unknown;
}

interface AdminAttractionLocationsFormValue {
  entrance?: AdminAttractionLocationPointFormValue | null;
  exit?: AdminAttractionLocationPointFormValue | null;
  fastPassEntrance?: AdminAttractionLocationPointFormValue | null;
  reducedMobilityEntrance?: AdminAttractionLocationPointFormValue | null;
}

interface AdminAttractionAccessConditionFormValue {
  type?: AttractionAccessConditionType | string | null;
  value?: unknown;
  unit?: AttractionAccessConditionUnit | string | null;
  requiresAccompaniment?: boolean | null;
  minimumCompanionAge?: unknown;
  label?: unknown;
  description?: unknown;
}

interface AdminAttractionDetailsFormValue {
  manufacturerId?: unknown;
  model?: unknown;
  openingDate?: unknown;
  closingDate?: unknown;
  durationInSeconds?: unknown;
  capacityPerHour?: unknown;
  heightInMeters?: unknown;
  lengthInMeters?: unknown;
  speedInKmH?: unknown;
  dropInMeters?: unknown;
  inversionCount?: unknown;
  trainCount?: unknown;
  carsPerTrain?: unknown;
  ridersPerVehicle?: unknown;
  hasSingleRider?: boolean | null;
  hasFastPass?: boolean | null;
  isAccessibleForReducedMobility?: boolean | null;
  isIndoor?: boolean | null;
  waterExposureLevel?: AttractionWaterExposureLevel | string | null;
  accessConditions?: Array<AdminAttractionAccessConditionFormValue> | null;
}

interface AdminParkItemFormValue {
  parkId: string;
  zoneId?: string | null;
  name: string;
  category: ParkItemCategory | string;
  type: ParkItemType | string;
  subtype?: string | null;
  latitude: unknown;
  longitude: unknown;
  descriptions?: LocalizedItem<string>[];
  attractionDetails?: AdminAttractionDetailsFormValue | null;
  attractionLocations?: AdminAttractionLocationsFormValue | null;
  isVisible?: boolean | null;
}
