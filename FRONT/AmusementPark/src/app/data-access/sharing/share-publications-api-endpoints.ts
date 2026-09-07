export const SHARE_PUBLICATIONS_API_ENDPOINTS = {
  preview: 'me/shares/preview',
  publish: 'me/shares/publish',
  visitSettings: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}/share`,
  sharedVisit: (shareId: string): string => `passport/shared/visits/${encodeURIComponent(shareId)}`
} as const;
