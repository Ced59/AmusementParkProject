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
  getArticle: (eventId: string) => `history/articles/${encodeURIComponent(eventId)}`,
  getAdminEvents: (query: AdminHistoryEventListQuery) => `admin/history/events${buildQuery({
    page: query.page ?? 1,
    size: query.size ?? 20,
    entityType: query.entityType ?? null,
    ownerId: query.ownerId ?? null,
    search: query.search ?? null,
    includeHidden: query.includeHidden ?? null
  })}`,
  createAdminEvent: 'admin/history/events',
  updateAdminEvent: (eventId: string) => `admin/history/events/${encodeURIComponent(eventId)}`,
  deleteAdminEvent: (eventId: string) => `admin/history/events/${encodeURIComponent(eventId)}`
};
