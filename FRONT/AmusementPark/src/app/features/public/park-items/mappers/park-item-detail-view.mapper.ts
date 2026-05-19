import { ImageDto } from '@app/models/images/image-dto';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { MapMarker } from '@app/models/map/map-marker';
import { AttractionAccessCondition } from '@app/models/parks/attraction-access-condition';
import { AttractionAccessConditionType } from '@app/models/parks/attraction-access-condition-type';
import { AttractionAccessConditionUnit } from '@app/models/parks/attraction-access-condition-unit';
import { AttractionLocationPoint } from '@app/models/parks/attraction-location-point';
import { AttractionLocations } from '@app/models/parks/attraction-locations';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { buildPublicParkItemsRouteCommands, buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import {
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  resolveParkItemRichDescription
} from '@shared/utils/display/park-item-presentation.helpers';
import { getLocalizedBooleanDisplay, normalizeTranslationSegment } from '@shared/utils/display/display-label.helpers';
import { resolveLocationMarkerIconKind } from '@shared/utils/maps/map-marker-icon-kind.resolver';
import { resolveLocalizedText } from '@shared/utils/localization/localized-text.helpers';
import { mapParkItemToCardViewModel } from './park-item-card.mapper';
import { ParkItemCardViewModel } from '../models/park-item-card.model';
import {
  ParkItemAccessConditionViewModel,
  ParkItemAccessConditionMetricViewModel,
  ParkItemDetailRowViewModel,
  ParkItemDetailSpecGroupViewModel,
  ParkItemDetailViewModel,
  ParkItemPhotoViewModel,
  ParkItemPhotoCategoryOptionViewModel,
  ParkItemLocationPointViewModel
} from '../models/park-item-detail-view.model';

const DEFAULT_MAP_CENTER: [number, number] = [48.8566, 2.3522];

export function mapParkItemToDetailViewModel(
  item: ParkItem | null,
  park: Park | null,
  manufacturerName: string | null,
  zoneName: string | null,
  currentLanguage: string,
  relatedItems: ParkItem[] = [],
  photos: ImageDto[] = [],
  imageTags: ImageTagDto[] = []
): ParkItemDetailViewModel | null {
  if (!item) {
    return null;
  }

  const technicalRows: ParkItemDetailRowViewModel[] = buildTechnicalRows(item, manufacturerName);
  const performanceRows: ParkItemDetailRowViewModel[] = buildPerformanceRows(item, currentLanguage);
  const experienceRows: ParkItemDetailRowViewModel[] = buildExperienceRows(item, currentLanguage);
  const specGroups: ParkItemDetailSpecGroupViewModel[] = buildSpecGroups(technicalRows, performanceRows, experienceRows);
  const locationPoints: ParkItemLocationPointViewModel[] = buildLocationPoints(item, currentLanguage);
  const hasPreciseLocations: boolean = locationPoints.some((point: ParkItemLocationPointViewModel) => !point.isGeneralFallback);
  const galleryPhotos: ParkItemPhotoViewModel[] = buildPhotos(photos, imageTags);

  return {
    name: item.name?.trim() ?? '',
    categoryLabelKey: getParkItemCategoryTranslationKey(item.category),
    typeLabelKey: getParkItemTypeTranslationKey(item.type),
    typeIconClass: resolveParkItemTypeIconClass(item.type),
    typeTone: resolveParkItemTypeTone(item.type, item.category),
    parkName: park?.name?.trim() ?? null,
    homeLink: ['/', currentLanguage, 'home'],
    parkLink: buildParkLink(park, currentLanguage),
    itemsLink: buildItemsLink(park, currentLanguage),
    description: resolveParkItemRichDescription(item, currentLanguage),
    manufacturerName,
    modelName: trimOrNull(item.attractionDetails?.model),
    status: trimOrNull(item.attractionDetails?.status),
    zoneName,
    subtype: trimOrNull(item.subtype),
    spotlightRows: buildSpotlightRows(item, performanceRows),
    summaryRows: buildSummaryRows(item, park, zoneName),
    specGroups,
    photos: galleryPhotos,
    photoCategories: buildPhotoCategories(galleryPhotos),
    heroPhoto: galleryPhotos.find((photo: ParkItemPhotoViewModel) => photo.isCurrent) ?? galleryPhotos[0] ?? null,
    accessConditions: buildAccessConditions(item, currentLanguage),
    locationPoints,
    mapMarkers: buildMapMarkers(locationPoints, item.name),
    mapCenter: resolveMapCenter(locationPoints, item, park),
    mapZoom: locationPoints.length > 1 ? 17 : 18,
    hasPreciseLocations,
    relatedItems: buildRelatedItems(item, park, relatedItems, currentLanguage, zoneName)
  };
}

function buildSpecGroups(
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

function buildTechnicalRows(item: ParkItem, manufacturerName: string | null): ParkItemDetailRowViewModel[] {
  const details = item.attractionDetails;
  const rows: ParkItemDetailRowViewModel[] = [];

  pushRow(rows, 'parkItems.fields.manufacturer', manufacturerName, null, 'pi pi-building');
  pushRow(rows, 'parkItems.fields.model', details?.model, null, 'pi pi-box');
  pushRow(rows, 'parkItems.fields.status', details?.status, null, 'pi pi-circle-fill');
  pushRow(rows, 'parkItems.fields.materialType', details?.materialType, null, 'pi pi-wrench');
  pushRow(rows, 'parkItems.fields.seatingType', details?.seatingType, null, 'pi pi-users');
  pushRow(rows, 'parkItems.fields.launchType', details?.launchType, null, 'pi pi-send');
  pushRow(rows, 'parkItems.fields.restraintType', details?.restraintType, null, 'pi pi-lock');
  pushRow(rows, 'parkItems.fields.openingDate', formatDate(details?.openingDate ?? details?.openingDateText), null, 'pi pi-calendar-plus');
  pushRow(rows, 'parkItems.fields.closingDate', formatDate(details?.closingDate ?? details?.closingDateText), null, 'pi pi-calendar-minus');

  return rows;
}

function buildPerformanceRows(item: ParkItem, currentLanguage: string): ParkItemDetailRowViewModel[] {
  const details = item.attractionDetails;
  const rows: ParkItemDetailRowViewModel[] = [];

  pushRow(rows, 'parkItems.fields.heightInMeters', formatNumberWithUnit(details?.heightInMeters, 'm'), null, 'pi pi-arrow-up');
  pushRow(rows, 'parkItems.fields.lengthInMeters', formatNumberWithUnit(details?.lengthInMeters, 'm'), null, 'pi pi-arrows-h');
  pushRow(rows, 'parkItems.fields.speedInKmH', formatNumberWithUnit(details?.speedInKmH, 'km/h'), null, 'pi pi-gauge');
  pushRow(rows, 'parkItems.fields.dropInMeters', formatNumberWithUnit(details?.dropInMeters, 'm'), null, 'pi pi-arrow-down');
  pushRow(rows, 'parkItems.fields.inversionCount', formatInteger(details?.inversionCount), null, 'pi pi-refresh');
  pushRow(rows, 'parkItems.fields.capacityPerHour', formatInteger(details?.capacityPerHour), null, 'pi pi-users');
  pushRow(rows, 'parkItems.fields.durationInSeconds', formatDuration(details?.durationInSeconds, currentLanguage), null, 'pi pi-clock');
  pushRow(rows, 'parkItems.fields.trainCount', formatInteger(details?.trainCount), null, 'pi pi-compass');
  pushRow(rows, 'parkItems.fields.carsPerTrain', formatInteger(details?.carsPerTrain), null, 'pi pi-th-large');
  pushRow(rows, 'parkItems.fields.ridersPerVehicle', formatInteger(details?.ridersPerVehicle), null, 'pi pi-user-plus');

  return rows;
}

function buildExperienceRows(item: ParkItem, currentLanguage: string): ParkItemDetailRowViewModel[] {
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

function buildSummaryRows(item: ParkItem, park: Park | null, zoneName: string | null): ParkItemDetailRowViewModel[] {
  const rows: ParkItemDetailRowViewModel[] = [];

  pushRow(rows, 'parkItems.fields.category', '', getParkItemCategoryTranslationKey(item.category), 'pi pi-tags');
  pushRow(rows, 'parkItems.fields.type', '', getParkItemTypeTranslationKey(item.type), 'pi pi-star');
  pushRow(rows, 'parkItems.fields.subtype', item.subtype, null, 'pi pi-tag');
  pushRow(rows, 'parkItems.fields.park', park?.name, null, 'pi pi-map');
  pushRow(rows, 'parkItems.fields.zone', zoneName, null, 'pi pi-map-marker');
  return rows;
}

function buildSpotlightRows(
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

  pushRow(rows, 'parkItems.fields.status', details?.status, null, 'pi pi-circle-fill');

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



function buildPhotos(photos: ImageDto[], imageTags: ImageTagDto[]): ParkItemPhotoViewModel[] {
  const tagSlugById: Map<string, string> = new Map<string, string>(imageTags.map((tag: ImageTagDto) => [tag.id, tag.slug]));
  return photos
    .filter((photo: ImageDto) => photo.isPublished !== false)
    .map((photo: ImageDto) => {
      const firstKnownTag: string | undefined = photo.tagIds
        ?.map((tagId: string) => tagSlugById.get(tagId) ?? tagId)
        .find((tagSlug: string) => !!tagSlug);
      const categoryKey: string = resolvePhotoCategoryKey(firstKnownTag);

      return {
        id: photo.id,
        imageId: photo.id,
        category: photo.category,
        categoryKey,
        categoryLabelKey: resolvePhotoCategoryLabelKey(categoryKey),
        description: trimOrNull(photo.description),
        alt: trimOrNull(photo.description) ?? trimOrNull(photo.originalFileName) ?? 'Park item photo',
        isCurrent: photo.isCurrent
      };
    });
}

function buildPhotoCategories(photos: ParkItemPhotoViewModel[]): ParkItemPhotoCategoryOptionViewModel[] {
  const countByCategoryKey: Map<string, number> = new Map<string, number>();

  for (const photo of photos) {
    countByCategoryKey.set(photo.categoryKey, (countByCategoryKey.get(photo.categoryKey) ?? 0) + 1);
  }

  return Array.from(countByCategoryKey.entries())
    .map(([key, count]: [string, number]) => ({
      key,
      labelKey: resolvePhotoCategoryLabelKey(key),
      count
    }))
    .sort((first: ParkItemPhotoCategoryOptionViewModel, second: ParkItemPhotoCategoryOptionViewModel) => {
      return getPhotoCategoryOrder(first.key) - getPhotoCategoryOrder(second.key);
    });
}

function resolvePhotoCategoryKey(tagIdOrSlug: string | undefined): string {
  const normalizedValue: string = (tagIdOrSlug ?? '').toLowerCase();

  if (normalizedValue.includes('entrance')) {
    return 'entrance';
  }

  if (normalizedValue.includes('exit')) {
    return 'exit';
  }

  if (normalizedValue.includes('layout')) {
    return 'layout';
  }

  if (normalizedValue.includes('queue')) {
    return 'queue';
  }

  if (normalizedValue.includes('station')) {
    return 'station';
  }

  return 'gallery';
}

function resolvePhotoCategoryLabelKey(categoryKey: string): string {
  return `parkItems.photos.categories.${categoryKey}`;
}

function getPhotoCategoryOrder(categoryKey: string): number {
  switch (categoryKey) {
    case 'gallery':
      return 0;
    case 'entrance':
      return 1;
    case 'exit':
      return 2;
    case 'queue':
      return 3;
    case 'station':
      return 4;
    case 'layout':
      return 5;
    default:
      return 99;
  }
}

function buildAccessConditions(item: ParkItem, currentLanguage: string): ParkItemAccessConditionViewModel[] {
  const conditions: AttractionAccessCondition[] = [...(item.attractionDetails?.accessConditions ?? [])]
    .sort((first: AttractionAccessCondition, second: AttractionAccessCondition) => (first.displayOrder ?? 0) - (second.displayOrder ?? 0));
  const viewModels: ParkItemAccessConditionViewModel[] = [];
  const heightCondition: ParkItemAccessConditionViewModel | null = buildHeightAccessCondition(conditions, currentLanguage);

  if (heightCondition) {
    viewModels.push(heightCondition);
  }

  viewModels.push(
    ...conditions
      .filter((condition: AttractionAccessCondition) => !isHeightAccessCondition(condition.type))
      .map((condition: AttractionAccessCondition) => mapAccessCondition(condition, currentLanguage))
  );

  return viewModels;
}

function buildHeightAccessCondition(
  conditions: AttractionAccessCondition[],
  currentLanguage: string
): ParkItemAccessConditionViewModel | null {
  const metrics: ParkItemAccessConditionMetricViewModel[] = [];

  pushHeightMetric(
    metrics,
    conditions,
    'MinHeight',
    'parkItems.accessConditions.height.minHeight',
    'parkItems.accessConditions.height.minHeightHelp',
    'pi pi-user',
    currentLanguage
  );
  pushHeightMetric(
    metrics,
    conditions,
    'MinHeightAccompanied',
    'parkItems.accessConditions.height.minHeightAccompanied',
    'parkItems.accessConditions.height.minHeightAccompaniedHelp',
    'pi pi-users',
    currentLanguage
  );
  pushHeightMetric(
    metrics,
    conditions,
    'MaxHeight',
    'parkItems.accessConditions.height.maxHeight',
    'parkItems.accessConditions.height.maxHeightHelp',
    'pi pi-ban',
    currentLanguage
  );

  if (metrics.length === 0) {
    return null;
  }

  return {
    title: null,
    titleKey: 'parkItems.accessConditions.height.title',
    description: null,
    rows: [],
    metrics,
    kind: 'height',
    iconClass: 'pi pi-arrows-v',
    tone: 'height'
  };
}

function pushHeightMetric(
  metrics: ParkItemAccessConditionMetricViewModel[],
  conditions: AttractionAccessCondition[],
  type: AttractionAccessConditionType,
  labelKey: string,
  helperKey: string,
  iconClass: string,
  currentLanguage: string
): void {
  const condition: AttractionAccessCondition | undefined = conditions.find((candidate: AttractionAccessCondition) => candidate.type === type);

  if (!condition || condition.value == null) {
    return;
  }

  metrics.push({
    labelKey,
    value: formatAccessConditionValue(condition.value, 'Centimeter', currentLanguage),
    helperKey,
    iconClass
  });
}

function mapAccessCondition(condition: AttractionAccessCondition, currentLanguage: string): ParkItemAccessConditionViewModel {
  const rows: ParkItemDetailRowViewModel[] = [];
  const title: string | null = resolveOptionalLocalizedText(condition.label, currentLanguage);
  const description: string | null = resolveOptionalLocalizedText(condition.description, currentLanguage);

  if (condition.value != null) {
    pushRow(rows, 'parkItems.accessConditionFields.value', formatAccessConditionValue(condition.value, condition.unit, currentLanguage), null, 'pi pi-sliders-h');
  }

  if (condition.requiresAccompaniment === true) {
    pushRow(rows, 'parkItems.accessConditionFields.requiresAccompaniment', formatBoolean(condition.requiresAccompaniment, currentLanguage), null, 'pi pi-users');
  }

  if (condition.minimumCompanionAge != null) {
    pushRow(rows, 'parkItems.accessConditionFields.minimumCompanionAge', formatAge(condition.minimumCompanionAge, currentLanguage), null, 'pi pi-user-plus');
  }

  return {
    title,
    titleKey: getAccessConditionTypeLabelKey(condition.type),
    description,
    rows,
    metrics: [],
    kind: getAccessConditionKind(condition.type),
    iconClass: getAccessConditionIconClass(condition.type),
    tone: getAccessConditionTone(condition.type)
  };
}

function isHeightAccessCondition(type: AttractionAccessConditionType): boolean {
  return type === 'MinHeight' || type === 'MinHeightAccompanied' || type === 'MaxHeight';
}

function getAccessConditionKind(type: AttractionAccessConditionType): 'restriction' | 'default' {
  switch (type) {
    case 'PregnancyRestriction':
    case 'HeartRestriction':
    case 'BackNeckRestriction':
    case 'WheelchairTransferRequired':
    case 'AccessPassRequired':
      return 'restriction';
    default:
      return 'default';
  }
}

function getAccessConditionIconClass(type: AttractionAccessConditionType): string {
  switch (type) {
    case 'MinAge':
    case 'MinAgeAccompanied':
      return 'pi pi-calendar';
    case 'PregnancyRestriction':
      return 'pi pi-exclamation-triangle';
    case 'HeartRestriction':
      return 'pi pi-heart';
    case 'BackNeckRestriction':
      return 'pi pi-shield';
    case 'WheelchairTransferRequired':
      return 'pi pi-directions';
    case 'AccessPassRequired':
      return 'pi pi-ticket';
    case 'Custom':
      return 'pi pi-info-circle';
    default:
      return 'pi pi-lock';
  }
}

function getAccessConditionTone(type: AttractionAccessConditionType): string {
  switch (type) {
    case 'PregnancyRestriction':
    case 'HeartRestriction':
    case 'BackNeckRestriction':
      return 'restriction';
    case 'WheelchairTransferRequired':
    case 'AccessPassRequired':
      return 'sky';
    case 'MinAge':
    case 'MinAgeAccompanied':
      return 'gold';
    default:
      return 'default';
  }
}

function buildLocationPoints(item: ParkItem, currentLanguage: string): ParkItemLocationPointViewModel[] {
  const precisePoints: ParkItemLocationPointViewModel[] = buildPreciseLocationPoints(item.attractionLocations, currentLanguage);

  if (precisePoints.length > 0) {
    return precisePoints;
  }

  if (!isValidCoordinatePair(item.latitude, item.longitude)) {
    return [];
  }

  return [{
    id: 'general',
    labelKey: 'parkItems.locations.general',
    iconClass: 'pi pi-map-marker',
    latitude: item.latitude,
    longitude: item.longitude,
    coordinatesLabel: formatCoordinates(item.latitude, item.longitude, currentLanguage),
    isGeneralFallback: true
  }];
}

function buildPreciseLocationPoints(locations: AttractionLocations | null | undefined, currentLanguage: string): ParkItemLocationPointViewModel[] {
  const points: ParkItemLocationPointViewModel[] = [];

  pushLocationPoint(points, 'entrance', 'parkItems.locations.entrance', 'pi pi-sign-in', locations?.entrance, currentLanguage);
  pushLocationPoint(points, 'exit', 'parkItems.locations.exit', 'pi pi-sign-out', locations?.exit, currentLanguage);
  pushLocationPoint(points, 'fastPassEntrance', 'parkItems.locations.fastPassEntrance', 'pi pi-ticket', locations?.fastPassEntrance, currentLanguage);
  pushLocationPoint(points, 'reducedMobilityEntrance', 'parkItems.locations.reducedMobilityEntrance', 'pi pi-heart', locations?.reducedMobilityEntrance, currentLanguage);

  return points;
}

function buildMapMarkers(points: ParkItemLocationPointViewModel[], itemName: string): MapMarker[] {
  return points.map((point: ParkItemLocationPointViewModel) => ({
    id: point.id,
    lat: point.latitude,
    lng: point.longitude,
    title: itemName,
    subtitle: point.coordinatesLabel,
    iconKind: resolveLocationMarkerIconKind(point.id),
    details: []
  }));
}

function buildRelatedItems(
  item: ParkItem,
  park: Park | null,
  relatedItems: ParkItem[],
  currentLanguage: string,
  zoneName: string | null
): ParkItemCardViewModel[] {
  return relatedItems
    .filter((candidate: ParkItem) => candidate.id !== item.id)
    .filter((candidate: ParkItem) => candidate.category === item.category || candidate.type === item.type || candidate.zoneId === item.zoneId)
    .slice(0, 3)
    .map((candidate: ParkItem) => mapParkItemToCardViewModel(
      candidate,
      park,
      currentLanguage,
      null,
      candidate.zoneId === item.zoneId ? zoneName : null
    ));
}

function resolveMapCenter(points: ParkItemLocationPointViewModel[], item: ParkItem, park: Park | null): [number, number] {
  const firstPoint: ParkItemLocationPointViewModel | undefined = points[0];

  if (firstPoint) {
    return [firstPoint.latitude, firstPoint.longitude];
  }

  if (isValidCoordinatePair(item.latitude, item.longitude)) {
    return [item.latitude, item.longitude];
  }

  if (park && isValidCoordinatePair(park.latitude, park.longitude)) {
    return [park.latitude, park.longitude];
  }

  return DEFAULT_MAP_CENTER;
}

function buildParkLink(park: Park | null, currentLanguage: string): string[] | null {
  return buildPublicParkRouteCommands({
    language: currentLanguage,
    parkId: park?.id,
    parkName: park?.name
  });
}

function buildItemsLink(park: Park | null, currentLanguage: string): string[] | null {
  return buildPublicParkItemsRouteCommands({
    language: currentLanguage,
    parkId: park?.id,
    parkName: park?.name
  });
}

function pushGroup(
  groups: ParkItemDetailSpecGroupViewModel[],
  titleKey: string,
  iconClass: string,
  rows: ParkItemDetailRowViewModel[]
): void {
  if (rows.length === 0) {
    return;
  }

  groups.push({ titleKey, iconClass, rows });
}

function pushRow(
  rows: ParkItemDetailRowViewModel[],
  labelKey: string,
  value: string | null | undefined,
  valueKey: string | null = null,
  iconClass: string | null = null
): void {
  const trimmedValue: string = value?.trim() ?? '';

  if (trimmedValue.length === 0 && !valueKey) {
    return;
  }

  rows.push({ labelKey, value: trimmedValue, valueKey, iconClass, isTextualValue: isTextualDetailValue(labelKey, trimmedValue, valueKey) });
}


function isTextualDetailValue(labelKey: string, value: string, valueKey: string | null): boolean {
  if (valueKey !== null) {
    return true;
  }

  if (labelKey === 'parkItems.fields.status') {
    return true;
  }

  return value.length > 8 && /[A-Za-zÀ-ÿ]/.test(value);
}

function pushLocationPoint(
  points: ParkItemLocationPointViewModel[],
  id: string,
  labelKey: string,
  iconClass: string,
  point: AttractionLocationPoint | null | undefined,
  currentLanguage: string
): void {
  if (!point || !isValidCoordinatePair(point.latitude, point.longitude)) {
    return;
  }

  points.push({
    id,
    labelKey,
    iconClass,
    latitude: point.latitude!,
    longitude: point.longitude!,
    coordinatesLabel: formatCoordinates(point.latitude!, point.longitude!, currentLanguage),
    isGeneralFallback: false
  });
}

function trimOrNull(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}

function formatDate(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';

  if (trimmedValue.length === 0) {
    return null;
  }

  return trimmedValue.length >= 10 ? trimmedValue.slice(0, 10) : trimmedValue;
}

function formatNumberWithUnit(value: number | null | undefined, unit: string): string | null {
  if (value == null) {
    return null;
  }

  return `${formatNumber(value)} ${unit}`;
}

function formatInteger(value: number | null | undefined): string | null {
  return value == null ? null : `${value}`;
}

function formatDuration(value: number | null | undefined, currentLanguage: string): string | null {
  if (value == null) {
    return null;
  }

  if (value < 60) {
    return `${value} s`;
  }

  const minutes: number = Math.floor(value / 60);
  const seconds: number = value % 60;
  const minuteLabel: string = currentLanguage === 'fr' ? 'min' : 'min';

  if (seconds === 0) {
    return `${minutes} ${minuteLabel}`;
  }

  return `${minutes} ${minuteLabel} ${seconds} s`;
}

function formatBoolean(value: boolean | null | undefined, currentLanguage: string): string | null {
  return getLocalizedBooleanDisplay(value, currentLanguage);
}

function formatAccessConditionValue(value: number, unit: AttractionAccessConditionUnit | null | undefined, currentLanguage: string): string {
  if (unit === 'Centimeter') {
    return `${formatNumber(value)} cm`;
  }

  if (unit === 'Year') {
    return formatAge(value, currentLanguage);
  }

  return formatNumber(value);
}

function formatAge(value: number, currentLanguage: string): string {
  const suffix: string = currentLanguage === 'fr' ? 'ans' : 'years';
  return `${formatNumber(value)} ${suffix}`;
}

function formatNumber(value: number): string {
  return Number.isInteger(value) ? `${value}` : `${value}`.replace('.', ',');
}

function formatCoordinates(latitude: number, longitude: number, currentLanguage: string): string {
  const separator: string = currentLanguage === 'fr' ? ' · ' : ' · ';
  return `${latitude.toFixed(5)}${separator}${longitude.toFixed(5)}`;
}

function isValidCoordinatePair(latitude: number | null | undefined, longitude: number | null | undefined): boolean {
  return latitude != null
    && longitude != null
    && Number.isFinite(latitude)
    && Number.isFinite(longitude)
    && Math.abs(latitude) <= 90
    && Math.abs(longitude) <= 180
    && !(latitude === 0 && longitude === 0);
}

function resolveOptionalLocalizedText(items: AttractionAccessCondition['label'], currentLanguage: string): string | null {
  const text: string = resolveLocalizedText(items, currentLanguage, '');
  return text.trim().length > 0 ? text : null;
}

function getAccessConditionTypeLabelKey(type: AttractionAccessConditionType): string {
  switch (type) {
    case 'MinHeight':
      return 'parkItems.accessConditionTypes.minHeight';
    case 'MinHeightAccompanied':
      return 'parkItems.accessConditionTypes.minHeightAccompanied';
    case 'MaxHeight':
      return 'parkItems.accessConditionTypes.maxHeight';
    case 'MinAge':
      return 'parkItems.accessConditionTypes.minAge';
    case 'MinAgeAccompanied':
      return 'parkItems.accessConditionTypes.minAgeAccompanied';
    case 'PregnancyRestriction':
      return 'parkItems.accessConditionTypes.pregnancyRestriction';
    case 'HeartRestriction':
      return 'parkItems.accessConditionTypes.heartRestriction';
    case 'BackNeckRestriction':
      return 'parkItems.accessConditionTypes.backNeckRestriction';
    case 'WheelchairTransferRequired':
      return 'parkItems.accessConditionTypes.wheelchairTransferRequired';
    case 'AccessPassRequired':
      return 'parkItems.accessConditionTypes.accessPassRequired';
    case 'Custom':
    default:
      return 'parkItems.accessConditionTypes.custom';
  }
}

function resolveParkItemTypeIconClass(type: string | null | undefined): string {
  switch (type) {
    case 'RollerCoaster':
      return 'pi pi-bolt';
    case 'WaterRide':
      return 'pi pi-compass';
    case 'FlatRide':
      return 'pi pi-sync';
    case 'DarkRide':
      return 'pi pi-moon';
    case 'FamilyRide':
      return 'pi pi-heart';
    case 'ThrillRide':
      return 'pi pi-send';
    case 'Restaurant':
    case 'Snack':
      return 'pi pi-shopping-bag';
    case 'Show':
      return 'pi pi-video';
    case 'Hotel':
      return 'pi pi-home';
    case 'Shop':
      return 'pi pi-shopping-cart';
    case 'Transport':
    case 'TransportRide':
      return 'pi pi-car';
    default:
      return 'pi pi-star';
  }
}

function resolveParkItemTypeTone(type: string | null | undefined, category: string | null | undefined): string {
  switch (type) {
    case 'RollerCoaster':
      return 'coaster';
    case 'WaterRide':
      return 'water';
    case 'FamilyRide':
    case 'Playground':
      return 'family';
    case 'Show':
      return 'show';
    case 'Restaurant':
    case 'Snack':
      return 'food';
    case 'ThrillRide':
      return 'thrill';
    default:
      return resolveParkItemCategoryTone(category);
  }
}

function resolveParkItemCategoryTone(category: string | null | undefined): string {
  switch (category) {
    case 'Restaurant':
      return 'food';
    case 'Hotel':
    case 'Service':
      return 'sky';
    case 'Show':
      return 'show';
    case 'Shop':
      return 'gold';
    case 'Transport':
      return 'family';
    default:
      return 'coaster';
  }
}
