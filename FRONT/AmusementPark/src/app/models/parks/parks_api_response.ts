import {Park} from "./park";
import {Pagination} from "../shared/pagination";

export interface ParksApiResponse {
  data: Park[];
  pagination: Pagination;
}
