import { FilterDefinition } from './filter-definition.model';
import { SortDefinition } from './sort-definition.model';

export interface ListQuery<TSortField extends string = string, TFilterKey extends string = string> {
  page: number;
  pageSize: number;
  searchTerm?: string | null;
  sort?: SortDefinition<TSortField> | null;
  filters?: readonly FilterDefinition<TFilterKey>[];
}
