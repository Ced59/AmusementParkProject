export const PASSPORT_VISITS_API_ENDPOINTS = {
  create: 'me/passport/visits',
  getById: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}`
};
