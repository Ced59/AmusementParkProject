import { PaginationContract } from './pagination.model';

export interface CollectionResponse<TItem> {
  data: TItem[];
  pagination: PaginationContract;
}
