import { PaginationContract } from './pagination.model';

export interface PagedResult<TItem> {
  items: TItem[];
  pagination: PaginationContract;
}
