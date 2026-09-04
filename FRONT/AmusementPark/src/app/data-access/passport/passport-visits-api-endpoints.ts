export const PASSPORT_VISITS_API_ENDPOINTS = {
  create: 'me/passport/visits',
  list: (limit: number, cursor: string | null): string => {
    const cursorQuery: string = cursor ? `&cursor=${encodeURIComponent(cursor)}` : '';
    return `me/passport/visits?limit=${limit}${cursorQuery}`;
  },
  getById: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}`,
  update: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}`,
  complete: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}/complete`,
  reopen: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}/reopen`,
  archive: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}/archive`,
  deletionPreview: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}/deletion-preview`,
  delete: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}`,
  assessment: (visitId: string): string => `me/passport/visits/${encodeURIComponent(visitId)}/assessment`
};
