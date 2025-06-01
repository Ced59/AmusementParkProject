import { SearchResultItem } from './search-result-item';
import { Pagination } from '../shared/pagination';

export interface SearchApiResponse {
  data: SearchResultItem[];
  pagination: Pagination;
}
