import {
  coalesceArray,
  createPagedResult,
  mapArray,
  mapCollectionResponse,
  mapNullable,
  normalizePagination
} from './mapping-helpers';

describe('mapping helpers', () => {
  it('coalesces nullish arrays to a new empty array and clones existing arrays', () => {
    const source: readonly number[] = [1, 2];
    const result: number[] = coalesceArray(source);

    expect(coalesceArray(null)).toEqual([]);
    expect(result).toEqual([1, 2]);
    expect(result).not.toBe(source);
  });

  it('maps arrays only after null coalescing', () => {
    expect(mapArray([1, 2], (item: number): string => `#${item}`)).toEqual(['#1', '#2']);
    expect(mapArray(null, (item: number): number => item * 2)).toEqual([]);
  });

  it('maps nullable values only when present', () => {
    expect(mapNullable(2, (value: number): number => value * 3)).toBe(6);
    expect(mapNullable(null, (value: number): number => value * 3)).toBeNull();
    expect(mapNullable(undefined, (value: number): number => value * 3)).toBeNull();
  });

  it('normalizes missing pagination from fallback item count', () => {
    expect(normalizePagination(null, 3)).toEqual({ totalItems: 3, totalPages: 1, currentPage: 1, itemsPerPage: 3 });
    expect(normalizePagination(null, 0)).toEqual({ totalItems: 0, totalPages: 0, currentPage: 1, itemsPerPage: 0 });
  });

  it('preserves explicit pagination fields including zero values', () => {
    expect(normalizePagination({ totalItems: 0, totalPages: 0, currentPage: 2, itemsPerPage: 10 }, 5))
      .toEqual({ totalItems: 0, totalPages: 0, currentPage: 2, itemsPerPage: 10 });
  });

  it('creates paged results from optional collections', () => {
    expect(createPagedResult(['a', 'b']).pagination.totalItems).toBe(2);
    expect(createPagedResult<string>(null).items).toEqual([]);
  });

  it('maps collection responses and handles null responses safely', () => {
    expect(mapCollectionResponse({ data: [1, 2], pagination: { totalItems: 2, totalPages: 1, currentPage: 1, itemsPerPage: 10 } }, (item: number): string => String(item)).items)
      .toEqual(['1', '2']);
    expect(mapCollectionResponse<number, string>(null, (item: number): string => String(item)).items).toEqual([]);
  });
});
