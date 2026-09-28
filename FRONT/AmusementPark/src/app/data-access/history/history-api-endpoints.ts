export interface AdminHistoryEventListQuery {
  page?: number;
  size?: number;
  entityType?: string | null;
  ownerId?: string | null;
  search?: string | null;
  includeHidden?: boolean | null;
}

function buildQuery(params: Record<string, string | number | boolean | null | undefined>): string {
  const query: string[] = [];

  for (const [key, value] of Object.entries(params)) {
    if (value === null || value === undefined || value === '') {
      continue;
    }

    query.push(`${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`);
  }

  return query.length > 0 ? `?${query.join('&')}` : '';
}

export const HISTORY_API_ENDPOINTS = {
  getPublicHistoricalLineage: (subjectType: string, subjectId: string, contextParkId: string) => `public/history/subjects/${encodeURIComponent(subjectType)}/${encodeURIComponent(subjectId)}/lineage${buildQuery({ contextParkId })}`,
  getPublicParkTimeline: (parkId: string, page: number = 1, pageSize: number = 50) => `public/parks/${encodeURIComponent(parkId)}/history/timeline${buildQuery({ page, pageSize })}`,
  getPublicParkSnapshot: (parkId: string, year: number, month?: number | null, day?: number | null) => `public/parks/${encodeURIComponent(parkId)}/history/snapshot${buildQuery({ year, month, day })}`,
  getPublicParkComparison: (parkId: string, fromYear: number, toYear: number) => `public/parks/${encodeURIComponent(parkId)}/history/compare${buildQuery({ fromYear, toYear })}`,
  getParkTimeline: (parkId: string, includeParkItems: boolean = false, parkItemIds: readonly string[] = [], page: number = 1) => {
    const params: string[] = [];

    if (includeParkItems) {
      params.push('includeParkItems=true');
    }

    if (page > 1) {
      params.push(`page=${encodeURIComponent(String(page))}`);
    }

    for (const parkItemId of parkItemIds) {
      if (parkItemId.trim().length > 0) {
        params.push(`parkItemIds=${encodeURIComponent(parkItemId.trim())}`);
      }
    }

    return `history/parks/${encodeURIComponent(parkId)}${params.length > 0 ? `?${params.join('&')}` : ''}`;
  },
  getParkItemTimeline: (parkItemId: string, page: number = 1) => `history/park-items/${encodeURIComponent(parkItemId)}${page > 1 ? `?page=${encodeURIComponent(String(page))}` : ''}`,
  getStandaloneAttractionTimeline: (standaloneAttractionId: string, page: number = 1) => `history/standalone-attractions/${encodeURIComponent(standaloneAttractionId)}${page > 1 ? `?page=${encodeURIComponent(String(page))}` : ''}`,
  getArticle: (eventId: string) => `history/articles/${encodeURIComponent(eventId)}`,
  getAdminEvents: (query: AdminHistoryEventListQuery) => `admin/history/events${buildQuery({
    page: query.page ?? 1,
    size: query.size ?? 20,
    entityType: query.entityType ?? null,
    ownerId: query.ownerId ?? null,
    search: query.search ?? null,
    includeHidden: query.includeHidden ?? null
  })}`,
  getAdminParkDiagnostics: (parkId: string) => `admin/history/parks/${encodeURIComponent(parkId)}/diagnostics`,
  getAdminParkWorkbench: (parkId: string) => `admin/history/parks/${encodeURIComponent(parkId)}/workbench`,
  previewAdminHistoricalImpact: (parkId: string) => `admin/history/parks/${encodeURIComponent(parkId)}/workbench/preview`,
  createAdminHistoricalSource: 'admin/history/sources',
  updateAdminHistoricalSource: (sourceId: string) => `admin/history/sources/${encodeURIComponent(sourceId)}`,
  reviewAdminHistoricalSource: (sourceId: string) => `admin/history/sources/${encodeURIComponent(sourceId)}/review`,
  retractAdminHistoricalSource: (sourceId: string) => `admin/history/sources/${encodeURIComponent(sourceId)}/retract`,
  createAdminHistoricalFact: (parkId: string) => `admin/history/parks/${encodeURIComponent(parkId)}/facts`,
  updateAdminHistoricalFact: (parkId: string, factId: string) => `admin/history/parks/${encodeURIComponent(parkId)}/facts/${encodeURIComponent(factId)}`,
  reviewAdminHistoricalFact: (factId: string) => `admin/history/facts/${encodeURIComponent(factId)}/review`,
  retractAdminHistoricalFact: (factId: string) => `admin/history/facts/${encodeURIComponent(factId)}/retract`,
  createAdminHistoricalRelation: (parkId: string) => `admin/history/parks/${encodeURIComponent(parkId)}/relations`,
  updateAdminHistoricalRelation: (parkId: string, relationId: string) => `admin/history/parks/${encodeURIComponent(parkId)}/relations/${encodeURIComponent(relationId)}`,
  reviewAdminHistoricalRelation: (relationId: string) => `admin/history/relations/${encodeURIComponent(relationId)}/review`,
  retractAdminHistoricalRelation: (relationId: string) => `admin/history/relations/${encodeURIComponent(relationId)}/retract`,
  createAdminEvent: 'admin/history/events',
  updateAdminEvent: (eventId: string) => `admin/history/events/${encodeURIComponent(eventId)}`,
  deleteAdminEvent: (eventId: string) => `admin/history/events/${encodeURIComponent(eventId)}`
};
