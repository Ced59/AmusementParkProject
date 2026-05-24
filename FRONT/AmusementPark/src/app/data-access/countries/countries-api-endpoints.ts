export const COUNTRIES_API_ENDPOINTS = {
  getCountries: (lang: string, page: number, size: number) => `countries?lang=${encodeURIComponent(lang)}&page=${page}&size=${size}`
};
