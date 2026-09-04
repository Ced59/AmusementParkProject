export const PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS = {
  validateTargets: 'me/passport/ride-targets:validate',
  list: (visitId: string, limit: number, cursor: string | null = null): string => {
    const encodedVisitId: string = encodeURIComponent(visitId);
    const cursorQuery: string = cursor ? `&cursor=${encodeURIComponent(cursor)}` : '';
    return `me/passport/visits/${encodedVisitId}/occurrences?limit=${limit}${cursorQuery}`;
  },
  addBatch: (visitId: string): string =>
    `me/passport/visits/${encodeURIComponent(visitId)}/occurrences:batch`,
  importBatch: (visitId: string): string =>
    `me/passport/visits/${encodeURIComponent(visitId)}/occurrences:import`,
  update: (visitId: string, occurrenceId: string): string =>
    `me/passport/visits/${encodeURIComponent(visitId)}/occurrences/${encodeURIComponent(occurrenceId)}`,
  get: (visitId: string, occurrenceId: string): string =>
    `me/passport/visits/${encodeURIComponent(visitId)}/occurrences/${encodeURIComponent(occurrenceId)}`,
  delete: (visitId: string, occurrenceId: string, expectedVersion: number): string =>
    `me/passport/visits/${encodeURIComponent(visitId)}/occurrences/${encodeURIComponent(occurrenceId)}?expectedVersion=${expectedVersion}`,
  reorder: (visitId: string): string =>
    `me/passport/visits/${encodeURIComponent(visitId)}/occurrences:reorder`,
  assessment: (occurrenceId: string): string =>
    `me/passport/occurrences/${encodeURIComponent(occurrenceId)}/assessment`,
  deleteAssessment: (occurrenceId: string, expectedVersion: number): string =>
    `me/passport/occurrences/${encodeURIComponent(occurrenceId)}/assessment?expectedVersion=${expectedVersion}`
};
