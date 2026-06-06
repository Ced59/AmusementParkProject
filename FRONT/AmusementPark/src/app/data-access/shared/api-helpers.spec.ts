import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';

import {
  normalizeParkItem,
  normalizeParkItemAdminRow,
  normalizeParkItemAdminRows,
  toImageCategoryApiValue,
  toImageOwnerTypeApiValue,
  unwrapCollection,
  unwrapPagedCollection
} from './api-helpers';

describe('api helpers', () => {
  it('unwraps arrays and paged collection responses to paged results', () => {
    expect(unwrapPagedCollection([1, 2]).items).toEqual([1, 2]);
    expect(unwrapPagedCollection({ data: [1], pagination: { totalItems: 10, totalPages: 5, currentPage: 2, itemsPerPage: 2 } }).pagination.currentPage).toBe(2);
    expect(unwrapPagedCollection<number>(null).items).toEqual([]);
  });

  it('unwraps arrays and collection response data to plain arrays', () => {
    expect(unwrapCollection(['a'])).toEqual(['a']);
    expect(unwrapCollection({ data: ['b'] })).toEqual(['b']);
    expect(unwrapCollection<string>(undefined)).toEqual([]);
  });

  it('normalizes numeric park item categories and types to string values', () => {
    const item: ParkItem = {
      parkId: 'p1',
      name: 'Ride',
      category: 0 as unknown as ParkItem['category'],
      type: 1 as unknown as ParkItem['type'],
      latitude: 0,
      longitude: 0
    };

    expect(normalizeParkItem(item).category).toBe('Attraction');
    expect(normalizeParkItem(item).type).toBe('RollerCoaster');
  });

  it('keeps existing string park item category and type values', () => {
    const item: ParkItem = {
      parkId: 'p1',
      name: 'Shop',
      category: 'Shop',
      type: 'Shop',
      latitude: 0,
      longitude: 0
    };

    expect(normalizeParkItem(item)).toEqual(item);
  });

  it('normalizes admin rows and arrays including null arrays', () => {
    const row: ParkItemAdminRow = {
      id: 'i1',
      name: 'Item',
      parkId: 'p1',
      parkName: 'Park',
      category: 6 as unknown as ParkItemAdminRow['category'],
      type: 25 as unknown as ParkItemAdminRow['type'],
      isVisible: true,
      adminReviewStatus: 'Validated'
    } as ParkItemAdminRow;

    expect(normalizeParkItemAdminRow(row).category).toBe('Service');
    expect(normalizeParkItemAdminRow(row).type).toBe('Parking');
    expect(normalizeParkItemAdminRows([row])[0].type).toBe('Parking');
    expect(normalizeParkItemAdminRows(null)).toEqual([]);
  });

  it('falls back to Other when numeric park item category or type is unknown', () => {
    const item: ParkItem = {
      parkId: 'p1',
      name: 'Unknown',
      category: 999 as unknown as ParkItem['category'],
      type: 999 as unknown as ParkItem['type'],
      latitude: 0,
      longitude: 0
    };

    const normalized: ParkItem = normalizeParkItem(item);

    expect(normalized.category).toBe('Other');
    expect(normalized.type).toBe('Other');
  });

  it('maps image owner types to API numeric values with default fallback', () => {
    expect(toImageOwnerTypeApiValue(ImageOwnerType.PARK)).toBe(1);
    expect(toImageOwnerTypeApiValue(ImageOwnerType.USER)).toBe(2);
    expect(toImageOwnerTypeApiValue(ImageOwnerType.PARK_FOUNDER)).toBe(6);
    expect(toImageOwnerTypeApiValue('unknown' as unknown as ImageOwnerType)).toBe(0);
  });

  it('maps image categories to API numeric values with default fallback', () => {
    expect(toImageCategoryApiValue(ImageCategory.AVATAR)).toBe(0);
    expect(toImageCategoryApiValue(ImageCategory.PARK_LOGO)).toBe(1);
    expect(toImageCategoryApiValue(ImageCategory.FOUNDER)).toBe(6);
    expect(toImageCategoryApiValue('unknown' as unknown as ImageCategory)).toBe(2);
  });
});
