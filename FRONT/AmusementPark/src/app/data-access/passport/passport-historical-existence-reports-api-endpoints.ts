export const PASSPORT_HISTORICAL_EXISTENCE_REPORTS_API_ENDPOINTS = {
  forVisit: (visitId: string): string =>
    `me/passport/visits/${encodeURIComponent(visitId)}/historical-existence-reports`
};
