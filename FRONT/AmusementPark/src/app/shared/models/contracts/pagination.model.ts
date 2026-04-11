export interface PaginationContract {
  totalItems: number;
  totalPages: number;
  currentPage: number;
  itemsPerPage: number;
}

export const DEFAULT_PAGINATION: PaginationContract = {
  totalItems: 0,
  totalPages: 0,
  currentPage: 1,
  itemsPerPage: 0,
};
