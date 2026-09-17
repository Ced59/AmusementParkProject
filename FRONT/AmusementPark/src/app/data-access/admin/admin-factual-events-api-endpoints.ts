export const ADMIN_FACTUAL_EVENTS_API_ENDPOINTS = {
  search: 'admin/factual-events',
  verify: (eventId: string): string => `admin/factual-events/${encodeURIComponent(eventId)}/verify`,
  publish: (eventId: string): string => `admin/factual-events/${encodeURIComponent(eventId)}/publish`,
  correct: (eventId: string): string => `admin/factual-events/${encodeURIComponent(eventId)}/correct`,
  retract: (eventId: string): string => `admin/factual-events/${encodeURIComponent(eventId)}/retract`,
} as const;
