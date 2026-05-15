export const SEARCH_API_ENDPOINTS = {
  getSearch: (query: string, categories: string[], page: number, size: number) => {
    const categoriesQuery: string = categories && categories.length > 0
      ? `&categories=${categories.join(',')}`
      : '';

    return `search?query=${encodeURIComponent(query)}${categoriesQuery}&page=${page}&pageSize=${size}`;
  }
};
