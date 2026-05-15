import { buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import {
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  resolveParkItemDescription
} from '@shared/utils/display/park-item-presentation.helpers';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { getLocalizedBooleanDisplay } from '@shared/utils/display/display-label.helpers';
import { ParkItemDetailRowViewModel, ParkItemDetailViewModel } from '../models/park-item-detail-view.model';

export function mapParkItemToDetailViewModel(
  item: ParkItem | null,
  park: Park | null,
  manufacturerName: string | null,
  zoneName: string | null,
  currentLanguage: string
): ParkItemDetailViewModel | null {
  if (!item) {
    return null;
  }

  const specRows: ParkItemDetailRowViewModel[] = buildSpecRows(item, manufacturerName, currentLanguage);
  const spotlightKeys: string[] = [
    'parkItems.fields.status',
    'parkItems.fields.heightInMeters',
    'parkItems.fields.speedInKmH',
    'parkItems.fields.inversionCount'
  ];
  const spotlightRows: ParkItemDetailRowViewModel[] = specRows
    .filter((row: ParkItemDetailRowViewModel) => spotlightKeys.includes(row.labelKey))
    .slice(0, 4);
  const spotlightKeySet: Set<string> = new Set(spotlightRows.map((row: ParkItemDetailRowViewModel) => row.labelKey));

  return {
    name: item.name?.trim() ?? '',
    categoryLabelKey: getParkItemCategoryTranslationKey(item.category),
    typeLabelKey: getParkItemTypeTranslationKey(item.type),
    parkName: park?.name?.trim() ?? null,
    parkLink: buildParkLink(park, currentLanguage),
    itemsLink: buildItemsLink(park, currentLanguage),
    description: resolveParkItemDescription(item, currentLanguage),
    manufacturerName,
    modelName: item.attractionDetails?.model?.trim() ?? null,
    status: item.attractionDetails?.status?.trim() ?? null,
    zoneName,
    sourceUrl: item.attractionDetails?.sourceUrl?.trim() ?? null,
    spotlightRows,
    secondaryRows: specRows.filter((row: ParkItemDetailRowViewModel) => !spotlightKeySet.has(row.labelKey))
  };
}

function buildSpecRows(
  item: ParkItem,
  manufacturerName: string | null,
  currentLanguage: string
): ParkItemDetailRowViewModel[] {
  const details = item.attractionDetails;

  if (!details) {
    return [];
  }

  const rows: ParkItemDetailRowViewModel[] = [];

  pushRow(rows, 'parkItems.fields.manufacturer', manufacturerName);
  pushRow(rows, 'parkItems.fields.model', details.model);
  pushRow(rows, 'parkItems.fields.status', details.status);
  pushRow(rows, 'parkItems.fields.materialType', details.materialType);
  pushRow(rows, 'parkItems.fields.seatingType', details.seatingType);
  pushRow(rows, 'parkItems.fields.launchType', details.launchType);
  pushRow(rows, 'parkItems.fields.restraintType', details.restraintType);
  pushRow(rows, 'parkItems.fields.openingDate', formatDate(details.openingDate ?? details.openingDateText));
  pushRow(rows, 'parkItems.fields.closingDate', formatDate(details.closingDate ?? details.closingDateText));
  pushRow(rows, 'parkItems.fields.heightInMeters', formatNumberWithUnit(details.heightInMeters, 'm'));
  pushRow(rows, 'parkItems.fields.heightInFeet', formatNumberWithUnit(details.heightInFeet, 'ft'));
  pushRow(rows, 'parkItems.fields.lengthInMeters', formatNumberWithUnit(details.lengthInMeters, 'm'));
  pushRow(rows, 'parkItems.fields.lengthInFeet', formatNumberWithUnit(details.lengthInFeet, 'ft'));
  pushRow(rows, 'parkItems.fields.speedInKmH', formatNumberWithUnit(details.speedInKmH, 'km/h'));
  pushRow(rows, 'parkItems.fields.speedInMph', formatNumberWithUnit(details.speedInMph, 'mph'));
  pushRow(rows, 'parkItems.fields.dropInMeters', formatNumberWithUnit(details.dropInMeters, 'm'));
  pushRow(rows, 'parkItems.fields.inversionCount', formatInteger(details.inversionCount));
  pushRow(rows, 'parkItems.fields.capacityPerHour', formatInteger(details.capacityPerHour));
  pushRow(rows, 'parkItems.fields.durationInSeconds', formatInteger(details.durationInSeconds));
  pushRow(rows, 'parkItems.fields.trainCount', formatInteger(details.trainCount));
  pushRow(rows, 'parkItems.fields.carsPerTrain', formatInteger(details.carsPerTrain));
  pushRow(rows, 'parkItems.fields.ridersPerVehicle', formatInteger(details.ridersPerVehicle));
  pushRow(rows, 'parkItems.fields.hasSingleRider', formatBoolean(details.hasSingleRider, currentLanguage));
  pushRow(rows, 'parkItems.fields.hasFastPass', formatBoolean(details.hasFastPass, currentLanguage));
  pushRow(rows, 'parkItems.fields.isAccessibleForReducedMobility', formatBoolean(details.isAccessibleForReducedMobility, currentLanguage));
  pushRow(rows, 'parkItems.fields.isIndoor', formatBoolean(details.isIndoor, currentLanguage));
  pushRow(rows, 'parkItems.fields.isLaunched', formatBoolean(details.isLaunched, currentLanguage));
  pushRow(rows, 'parkItems.fields.waterExposureLevel', details.waterExposureLevel ?? null);

  return rows;
}

function buildParkLink(park: Park | null, currentLanguage: string): string[] | null {
  if (!park?.id || !park?.name) {
    return null;
  }

  return ['/', currentLanguage, 'park', park.id, buildParkSlug(park.name)];
}

function buildItemsLink(park: Park | null, currentLanguage: string): string[] | null {
  if (!park?.id || !park?.name) {
    return null;
  }

  return ['/', currentLanguage, 'park', park.id, buildParkSlug(park.name), 'items'];
}

function pushRow(rows: ParkItemDetailRowViewModel[], labelKey: string, value: string | null | undefined): void {
  if (value == null || value.trim() === '') {
    return;
  }

  rows.push({ labelKey, value });
}

function formatDate(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }

  return value.length >= 10 ? value.slice(0, 10) : value;
}

function formatNumberWithUnit(value: number | null | undefined, unit: string): string | null {
  if (value == null) {
    return null;
  }

  return `${value} ${unit}`;
}

function formatInteger(value: number | null | undefined): string | null {
  return value == null ? null : `${value}`;
}

function formatBoolean(value: boolean | null | undefined, currentLanguage: string): string | null {
  return getLocalizedBooleanDisplay(value, currentLanguage);
}
