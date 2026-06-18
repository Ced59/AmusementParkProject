import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { MeasurementSystem } from '@shared/models/measurements/measurement-system.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { normalizeTranslationSegment } from '@shared/utils/display/display-label.helpers';
import {
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey
} from '@shared/utils/display/park-item-presentation.helpers';
import { buildPublicParkReferenceRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import {
  ParkItemDetailRowViewModel,
  ParkItemDetailSpecGroupViewModel
} from '../models/park-item-detail-view.model';
import {
  formatBoolean,
  formatDate,
  formatDuration,
  formatInteger,
  formatLengthFromMeters,
  formatSpeedFromKilometersPerHour
} from './park-item-detail-formatters';
import {
  buildCategoryQueryParams,
  buildItemsLink,
  buildParkLink,
  buildSearchQueryParams,
  buildTypeQueryParams,
  buildZoneQueryParams
} from './park-item-detail-navigation.mapper';
import { getAttractionStatusValueKey } from './park-item-detail-presentation.mapper';
import { pushGroup, pushRow } from './park-item-detail-row.helpers';

export function buildSpecGroups(
  technicalRows: ParkItemDetailRowViewModel[],
  performanceRows: ParkItemDetailRowViewModel[],
  experienceRows: ParkItemDetailRowViewModel[]
): ParkItemDetailSpecGroupViewModel[] {
  const groups: ParkItemDetailSpecGroupViewModel[] = [];

  pushGroup(groups, 'parkItems.detail.technicalTitle', 'pi pi-cog', technicalRows);
  pushGroup(groups, 'parkItems.detail.performanceTitle', 'pi pi-bolt', performanceRows);
  pushGroup(groups, 'parkItems.detail.experienceTitle', 'pi pi-sparkles', experienceRows);

  return groups;
}

export function buildTechnicalRows(item: ParkItem, manufacturerName: string | null, currentLanguage: string): ParkItemDetailRowViewModel[] {
  const details = item.attractionDetails;
  const rows: ParkItemDetailRowViewModel[] = [];

  pushRow(
    rows,
    'parkItems.fields.manufacturer',
    manufacturerName,
    null,
    'pi pi-building',
    item.attractionDetails?.manufacturerId && manufacturerName
      ? buildPublicParkReferenceRouteCommands({
        language: currentLanguage,
        referenceId: item.attractionDetails.manufacturerId,
        referenceName: manufacturerName,
        kind: 'manufacturer'
      })
      : null);
  pushRow(rows, 'parkItems.fields.model', details?.model, null, 'pi pi-box');
  pushStatusRow(rows, details?.status);
  pushRow(rows, 'parkItems.fields.materialType', details?.materialType, null, 'pi pi-wrench');
  pushRow(rows, 'parkItems.fields.seatingType', details?.seatingType, null, 'pi pi-users');
  pushRow(rows, 'parkItems.fields.launchType', details?.launchType, null, 'pi pi-send');
  pushRow(rows, 'parkItems.fields.restraintType', details?.restraintType, null, 'pi pi-lock');
  pushRow(rows, 'parkItems.fields.openingDate', formatDate(details?.openingDate ?? details?.openingDateText), null, 'pi pi-calendar-plus');
  pushRow(rows, 'parkItems.fields.closingDate', formatDate(details?.closingDate ?? details?.closingDateText), null, 'pi pi-calendar-minus');

  return rows;
}

function pushStatusRow(rows: ParkItemDetailRowViewModel[], status: string | null | undefined): void {
  const statusValueKey: string | null = getAttractionStatusValueKey(status);
  pushRow(
    rows,
    'parkItems.fields.status',
    statusValueKey ? '' : status,
    statusValueKey,
    'pi pi-circle-fill'
  );
}

export function buildPerformanceRows(
  item: ParkItem,
  currentLanguage: string,
  measurementSystem: MeasurementSystem,
  measurementConversionService: MeasurementConversionService
): ParkItemDetailRowViewModel[] {
  const details = item.attractionDetails;
  const rows: ParkItemDetailRowViewModel[] = [];

  pushRow(rows, 'parkItems.fields.heightInMeters', formatLengthFromMeters(details?.heightInMeters, currentLanguage, measurementSystem, measurementConversionService), null, 'pi pi-arrow-up');
  pushRow(rows, 'parkItems.fields.lengthInMeters', formatLengthFromMeters(details?.lengthInMeters, currentLanguage, measurementSystem, measurementConversionService), null, 'pi pi-arrows-h');
  pushRow(rows, 'parkItems.fields.speedInKmH', formatSpeedFromKilometersPerHour(details?.speedInKmH, currentLanguage, measurementSystem, measurementConversionService), null, 'pi pi-gauge');
  pushRow(rows, 'parkItems.fields.dropInMeters', formatLengthFromMeters(details?.dropInMeters, currentLanguage, measurementSystem, measurementConversionService), null, 'pi pi-arrow-down');
  pushRow(rows, 'parkItems.fields.inversionCount', formatInteger(details?.inversionCount), null, 'pi pi-refresh');
  pushRow(rows, 'parkItems.fields.capacityPerHour', formatInteger(details?.capacityPerHour), null, 'pi pi-users');
  pushRow(rows, 'parkItems.fields.durationInSeconds', formatDuration(details?.durationInSeconds, currentLanguage), null, 'pi pi-clock');
  pushRow(rows, 'parkItems.fields.trainCount', formatInteger(details?.trainCount), null, 'pi pi-compass');
  pushRow(rows, 'parkItems.fields.carsPerTrain', formatInteger(details?.carsPerTrain), null, 'pi pi-th-large');
  pushRow(rows, 'parkItems.fields.ridersPerVehicle', formatInteger(details?.ridersPerVehicle), null, 'pi pi-user-plus');

  return rows;
}

export function buildExperienceRows(item: ParkItem, currentLanguage: string): ParkItemDetailRowViewModel[] {
  const details = item.attractionDetails;
  const rows: ParkItemDetailRowViewModel[] = [];

  pushRow(rows, 'parkItems.fields.hasSingleRider', formatBoolean(details?.hasSingleRider, currentLanguage), null, 'pi pi-user');
  pushRow(rows, 'parkItems.fields.hasFastPass', formatBoolean(details?.hasFastPass, currentLanguage), null, 'pi pi-ticket');
  pushRow(rows, 'parkItems.fields.isAccessibleForReducedMobility', formatBoolean(details?.isAccessibleForReducedMobility, currentLanguage), null, 'pi pi-heart');
  pushRow(rows, 'parkItems.fields.isIndoor', formatBoolean(details?.isIndoor, currentLanguage), null, 'pi pi-home');
  pushRow(rows, 'parkItems.fields.isLaunched', formatBoolean(details?.isLaunched, currentLanguage), null, 'pi pi-send');

  if (details?.waterExposureLevel) {
    pushRow(
      rows,
      'parkItems.fields.waterExposureLevel',
      '',
      `parkItems.waterExposureLevels.${normalizeTranslationSegment(details.waterExposureLevel, 'none')}`,
      'pi pi-cloud'
    );
  }

  return rows;
}

export function buildSummaryRows(
  item: ParkItem,
  park: Park | null,
  manufacturerName: string | null,
  zoneName: string | null,
  currentLanguage: string
): ParkItemDetailRowViewModel[] {
  const rows: ParkItemDetailRowViewModel[] = [];
  const itemsLink: string[] | null = buildItemsLink(park, currentLanguage);

  pushRow(
    rows,
    'parkItems.fields.category',
    '',
    getParkItemCategoryTranslationKey(item.category),
    'pi pi-tags',
    itemsLink,
    buildCategoryQueryParams(item.category)
  );
  pushRow(
    rows,
    'parkItems.fields.type',
    '',
    getParkItemTypeTranslationKey(item.type),
    'pi pi-star',
    itemsLink,
    buildTypeQueryParams(item.type)
  );
  pushRow(
    rows,
    'parkItems.fields.subtype',
    item.subtype,
    null,
    'pi pi-tag',
    itemsLink,
    buildSearchQueryParams(item.subtype)
  );
  pushRow(
    rows,
    'parkItems.fields.manufacturer',
    manufacturerName,
    null,
    'pi pi-building',
    item.attractionDetails?.manufacturerId && manufacturerName
      ? buildPublicParkReferenceRouteCommands({
        language: currentLanguage,
        referenceId: item.attractionDetails.manufacturerId,
        referenceName: manufacturerName,
        kind: 'manufacturer'
      })
      : null
  );
  pushRow(rows, 'parkItems.fields.park', park?.name, null, 'pi pi-map', buildParkLink(park, currentLanguage));
  pushRow(rows, 'parkItems.fields.zone', zoneName, null, 'pi pi-map-marker', itemsLink, buildZoneQueryParams(item.zoneId));

  return rows;
}

export function buildSpotlightRows(
  item: ParkItem,
  performanceRows: ParkItemDetailRowViewModel[]
): ParkItemDetailRowViewModel[] {
  const details = item.attractionDetails;
  const preferredKeys: string[] = [
    'parkItems.fields.status',
    'parkItems.fields.heightInMeters',
    'parkItems.fields.speedInKmH',
    'parkItems.fields.inversionCount',
    'parkItems.fields.durationInSeconds'
  ];
  const rows: ParkItemDetailRowViewModel[] = [];

  pushStatusRow(rows, details?.status);

  for (const key of preferredKeys) {
    const row: ParkItemDetailRowViewModel | undefined = performanceRows.find((candidate: ParkItemDetailRowViewModel) => candidate.labelKey === key);

    if (row && rows.every((existing: ParkItemDetailRowViewModel) => existing.labelKey !== row.labelKey)) {
      rows.push(row);
    }
  }

  if (rows.length > 0) {
    return rows.slice(0, 4);
  }

  return [
    {
      labelKey: 'parkItems.fields.category',
      value: '',
      valueKey: getParkItemCategoryTranslationKey(item.category),
      iconClass: 'pi pi-tags',
      isTextualValue: true
    },
    {
      labelKey: 'parkItems.fields.type',
      value: '',
      valueKey: getParkItemTypeTranslationKey(item.type),
      iconClass: 'pi pi-star',
      isTextualValue: true
    },
  ].filter((row: ParkItemDetailRowViewModel) => row.value.length > 0 || !!row.valueKey);
}
