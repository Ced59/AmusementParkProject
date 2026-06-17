import { PagedResult, PaginationContract } from '@shared/models/contracts';
import { coalesceArray, createPagedResult, mapArray } from '@shared/utils/mapping';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { AttractionLocationPoint } from '@app/models/parks/attraction-location-point';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';

const IMAGE_OWNER_TYPE_API_VALUES: ReadonlyMap<ImageOwnerType, number> = new Map<ImageOwnerType, number>([
  [ImageOwnerType.PARK, 1],
  [ImageOwnerType.USER, 2],
  [ImageOwnerType.PARK_ITEM, 3],
  [ImageOwnerType.PARK_OPERATOR, 4],
  [ImageOwnerType.ATTRACTION_MANUFACTURER, 5],
  [ImageOwnerType.PARK_FOUNDER, 6],
  [ImageOwnerType.VIDEO, 7]
]);

const IMAGE_CATEGORY_API_VALUES: ReadonlyMap<ImageCategory, number> = new Map<ImageCategory, number>([
  [ImageCategory.AVATAR, 0],
  [ImageCategory.PARK_LOGO, 1],
  [ImageCategory.PARK, 2],
  [ImageCategory.PARK_ITEM, 3],
  [ImageCategory.OPERATOR, 4],
  [ImageCategory.MANUFACTURER, 5],
  [ImageCategory.FOUNDER, 6],
  [ImageCategory.VIDEO_THUMBNAIL, 7]
]);

const PARK_ITEM_CATEGORIES_BY_API_VALUE: ReadonlyMap<number, ParkItem['category']> = new Map<number, ParkItem['category']>([
  [0, 'Attraction'],
  [1, 'Restaurant'],
  [2, 'Hotel'],
  [3, 'Animal'],
  [4, 'Show'],
  [5, 'Shop'],
  [6, 'Service'],
  [7, 'Transport']
]);

const PARK_ITEM_TYPES_BY_API_VALUE: ReadonlyMap<number, ParkItem['type']> = new Map<number, ParkItem['type']>([
  [0, 'Attraction'],
  [1, 'RollerCoaster'],
  [2, 'WaterRide'],
  [3, 'FlatRide'],
  [4, 'DarkRide'],
  [5, 'FamilyRide'],
  [6, 'ThrillRide'],
  [7, 'TransportRide'],
  [8, 'WalkThrough'],
  [9, 'Playground'],
  [10, 'InteractiveExperience'],
  [11, 'ObservationRide'],
  [12, 'AnimalExhibit'],
  [13, 'Restaurant'],
  [14, 'Snack'],
  [15, 'Hotel'],
  [16, 'Show'],
  [17, 'Shop'],
  [18, 'Game'],
  [19, 'MeetAndGreet'],
  [20, 'Service'],
  [21, 'Toilets'],
  [22, 'FirstAid'],
  [23, 'Information'],
  [24, 'Locker'],
  [25, 'Parking'],
  [26, 'Transport'],
  [27, 'Station'],
  [28, 'Other'],
  [29, 'Cinema']
]);


export interface PagedCollectionResponse<T> {
  data?: T[];
  items?: T[];
  pagination?: PaginationContract | null;
}


export function unwrapPagedCollection<T>(response: T[] | PagedCollectionResponse<T> | null | undefined): PagedResult<T> {
  if (Array.isArray(response)) {
    return createPagedResult<T>(response);
  }

  if (response) {
    const items: T[] | undefined = Array.isArray(response.data)
      ? response.data
      : Array.isArray(response.items)
        ? response.items
        : undefined;

    if (items) {
      return createPagedResult<T>(items, response.pagination);
    }
  }

  return createPagedResult<T>([]);
}

export function unwrapCollection<T>(response: T[] | PagedCollectionResponse<T> | null | undefined): T[] {
  if (Array.isArray(response)) {
    return response;
  }

  if (response) {
    if (Array.isArray(response.data)) {
      return coalesceArray(response.data);
    }

    if (Array.isArray(response.items)) {
      return coalesceArray(response.items);
    }
  }

  return [];
}

export function normalizeParkItem(item: ParkItem): ParkItem {
  const fallbackPosition: AttractionLocationPoint | null = resolveParkItemFallbackPosition(item);

  return {
    ...item,
    latitude: normalizeCoordinate(item.latitude, fallbackPosition?.latitude),
    longitude: normalizeCoordinate(item.longitude, fallbackPosition?.longitude),
    category: toParkItemCategory(item.category),
    type: toParkItemType(item.type)
  };
}

export function normalizeParkItemAdminRow(row: ParkItemAdminRow): ParkItemAdminRow {
  return {
    ...row,
    category: toParkItemCategory(row.category),
    type: toParkItemType(row.type),
    contentQuality: row.contentQuality ? {
      ...row.contentQuality,
      availableLanguageCodes: row.contentQuality.availableLanguageCodes ?? [],
      missingRequirementKeys: row.contentQuality.missingRequirementKeys ?? []
    } : undefined,
    publicationSignals: row.publicationSignals ? {
      ...row.publicationSignals,
      availableLanguageCodes: row.publicationSignals.availableLanguageCodes ?? []
    } : undefined
  };
}

export function normalizeParkItemAdminRows(rows: ParkItemAdminRow[] | null | undefined): ParkItemAdminRow[] {
  return mapArray(rows, (row: ParkItemAdminRow) => normalizeParkItemAdminRow(row));
}

export function toImageOwnerTypeApiValue(value: ImageOwnerType): number {
  return IMAGE_OWNER_TYPE_API_VALUES.get(value) ?? 0;
}

export function toImageCategoryApiValue(value: ImageCategory): number {
  return IMAGE_CATEGORY_API_VALUES.get(value) ?? 2;
}

function toParkItemCategory(value: ParkItem['category'] | ParkItemAdminRow['category'] | number | null | undefined): ParkItem['category'] {
  if (typeof value === 'string') {
    return value as ParkItem['category'];
  }

  return PARK_ITEM_CATEGORIES_BY_API_VALUE.get(Number(value)) ?? 'Other';
}

function toParkItemType(value: ParkItem['type'] | ParkItemAdminRow['type'] | number | null | undefined): ParkItem['type'] {
  if (typeof value === 'string') {
    return value as ParkItem['type'];
  }

  return PARK_ITEM_TYPES_BY_API_VALUE.get(Number(value)) ?? 'Other';
}

function resolveParkItemFallbackPosition(item: ParkItem): AttractionLocationPoint | null {
  return item.attractionLocations?.entrance
    ?? item.attractionLocations?.exit
    ?? item.attractionLocations?.fastPassEntrance
    ?? item.attractionLocations?.reducedMobilityEntrance
    ?? null;
}

function normalizeCoordinate(primary: number | null | undefined, fallback: number | null | undefined): number | null {
  if (primary !== null && primary !== undefined) {
    return primary;
  }

  return fallback ?? null;
}
