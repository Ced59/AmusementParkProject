import { PagedResult, PaginationContract } from '@shared/models/contracts';
import { coalesceArray, createPagedResult, mapArray } from '@shared/utils/mapping';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';

export interface PagedCollectionResponse<T> {
  data?: T[];
  pagination?: PaginationContract | null;
}


export function unwrapPagedCollection<T>(response: T[] | PagedCollectionResponse<T> | null | undefined): PagedResult<T> {
  if (Array.isArray(response)) {
    return createPagedResult<T>(response);
  }

  if (response && Array.isArray(response.data)) {
    return createPagedResult<T>(response.data, response.pagination);
  }

  return createPagedResult<T>([]);
}

export function unwrapCollection<T>(response: T[] | PagedCollectionResponse<T> | null | undefined): T[] {
  if (Array.isArray(response)) {
    return response;
  }

  if (response && Array.isArray(response.data)) {
    return coalesceArray(response.data);
  }

  return [];
}

export function normalizeParkItem(item: ParkItem): ParkItem {
  return {
    ...item,
    category: toParkItemCategory(item.category),
    type: toParkItemType(item.type)
  };
}

export function normalizeParkItemAdminRow(row: ParkItemAdminRow): ParkItemAdminRow {
  return {
    ...row,
    category: toParkItemCategory(row.category),
    type: toParkItemType(row.type)
  };
}

export function normalizeParkItemAdminRows(rows: ParkItemAdminRow[] | null | undefined): ParkItemAdminRow[] {
  return mapArray(rows, (row: ParkItemAdminRow) => normalizeParkItemAdminRow(row));
}

export function toImageOwnerTypeApiValue(value: ImageOwnerType): number {
  switch (value) {
    case ImageOwnerType.PARK:
      return 1;
    case ImageOwnerType.USER:
      return 2;
    case ImageOwnerType.ATTRACTION:
      return 3;
    default:
      return 0;
  }
}

export function toImageCategoryApiValue(value: ImageCategory): number {
  switch (value) {
    case ImageCategory.AVATAR:
      return 0;
    case ImageCategory.PARK_LOGO:
      return 1;
    case ImageCategory.PARK:
      return 2;
    case ImageCategory.ATTRACTION:
      return 3;
    default:
      return 2;
  }
}

function toParkItemCategory(value: ParkItem['category'] | ParkItemAdminRow['category'] | number | null | undefined): ParkItem['category'] {
  if (typeof value === 'string') {
    return value as ParkItem['category'];
  }

  switch (value) {
    case 0:
      return 'Attraction';
    case 1:
      return 'Restaurant';
    case 2:
      return 'Hotel';
    case 3:
      return 'Animal';
    case 4:
      return 'Show';
    case 5:
      return 'Shop';
    case 6:
      return 'Service';
    case 7:
      return 'Transport';
    default:
      return 'Other';
  }
}

function toParkItemType(value: ParkItem['type'] | ParkItemAdminRow['type'] | number | null | undefined): ParkItem['type'] {
  if (typeof value === 'string') {
    return value as ParkItem['type'];
  }

  switch (value) {
    case 0:
      return 'Attraction';
    case 1:
      return 'RollerCoaster';
    case 2:
      return 'WaterRide';
    case 3:
      return 'FlatRide';
    case 4:
      return 'DarkRide';
    case 5:
      return 'FamilyRide';
    case 6:
      return 'ThrillRide';
    case 7:
      return 'TransportRide';
    case 8:
      return 'WalkThrough';
    case 9:
      return 'Playground';
    case 10:
      return 'InteractiveExperience';
    case 11:
      return 'ObservationRide';
    case 12:
      return 'AnimalExhibit';
    case 13:
      return 'Restaurant';
    case 14:
      return 'Snack';
    case 15:
      return 'Hotel';
    case 16:
      return 'Show';
    case 17:
      return 'Shop';
    case 18:
      return 'Service';
    case 19:
      return 'Transport';
    default:
      return 'Other';
  }
}
