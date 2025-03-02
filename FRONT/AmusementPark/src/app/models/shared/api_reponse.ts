import {Pagination} from "./pagination";

export interface ApiResponse<T> {
  data: T[];
  pagination: Pagination;
}
