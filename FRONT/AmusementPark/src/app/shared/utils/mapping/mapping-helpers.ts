import {
  CollectionResponse,
  DEFAULT_PAGINATION,
  PagedResult,
  PaginationContract,
} from '@shared/models/contracts';

export type Mapper<TSource, TTarget> = (source: TSource) => TTarget;

export function coalesceArray<TItem>(items: readonly TItem[] | null | undefined): TItem[] {
  return items == null ? [] : [...items];
}

export function mapArray<TSource, TTarget>(
  items: readonly TSource[] | null | undefined,
  mapper: Mapper<TSource, TTarget>
): TTarget[] {
  return coalesceArray(items).map((item: TSource) => mapper(item));
}

export function mapNullable<TSource, TTarget>(
  value: TSource | null | undefined,
  mapper: Mapper<TSource, TTarget>
): TTarget | null {
  if (value == null) {
    return null;
  }

  return mapper(value);
}

export function normalizePagination(
  pagination: Partial<PaginationContract> | null | undefined,
  fallbackItemCount: number = 0
): PaginationContract {
  return {
    totalItems: pagination?.totalItems ?? fallbackItemCount,
    totalPages: pagination?.totalPages ?? (fallbackItemCount > 0 ? 1 : 0),
    currentPage: pagination?.currentPage ?? DEFAULT_PAGINATION.currentPage,
    itemsPerPage: pagination?.itemsPerPage ?? fallbackItemCount,
  };
}

export function createPagedResult<TItem>(
  items: readonly TItem[] | null | undefined,
  pagination?: Partial<PaginationContract> | null
): PagedResult<TItem> {
  const normalizedItems: TItem[] = coalesceArray(items);

  return {
    items: normalizedItems,
    pagination: normalizePagination(pagination, normalizedItems.length),
  };
}

export function mapCollectionResponse<TSource, TTarget>(
  response: CollectionResponse<TSource> | null | undefined,
  mapper: Mapper<TSource, TTarget>
): PagedResult<TTarget> {
  if (response == null) {
    return createPagedResult<TTarget>([]);
  }

  return createPagedResult<TTarget>(mapArray(response.data, mapper), response.pagination);
}
