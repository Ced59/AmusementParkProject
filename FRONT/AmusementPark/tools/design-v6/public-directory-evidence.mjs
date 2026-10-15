// Evidence only: do not persist full DTOs or accept private/non-directory reads.
export function publicDirectoryReadEvidence(url, status, bytes) {
  const target = new URL(url, 'https://amusement-parks.fun');
  if (target.pathname !== '/api/parks' || target.searchParams.get('visibleOnly') !== 'true' || status !== 200) return undefined;
  return publicPagedVisibleEvidence(bytes);
}

export function publicParkItemsReadEvidence(url, status, bytes) {
  const target = new URL(url, 'https://amusement-parks.fun');
  if (!/^\/api\/park-items\/park\/[0-9a-f-]{36}$/i.test(target.pathname) || status !== 200) return undefined;
  if (target.searchParams.has('includeHidden') && target.searchParams.get('includeHidden') !== 'false') {
    throw new Error('Hidden item evidence is prohibited.');
  }
  return publicPagedVisibleEvidence(bytes);
}

function publicPagedVisibleEvidence(bytes) {
  const payload = JSON.parse(Buffer.from(bytes).toString('utf8'));
  const pagination = payload.pagination;
  if (!Array.isArray(payload.data) || payload.data.length > 100
    || payload.data.some(park => park.isVisible !== true || typeof park.name !== 'string' || !park.name.trim())
    || !pagination || !['currentPage', 'itemsPerPage', 'totalItems'].every(key => Number.isInteger(pagination[key]))
    || pagination.currentPage < 1 || pagination.itemsPerPage < 1
    || pagination.itemsPerPage < payload.data.length || pagination.totalItems < payload.data.length) {
    throw new Error('Invalid or non-public directory evidence; review the bounded public fixture.');
  }
  return {
    names: payload.data.map(park => park.name.trim()),
    pagination: { currentPage: pagination.currentPage, itemsPerPage: pagination.itemsPerPage, totalItems: pagination.totalItems },
  };
}

export function publicRouteEvidenceName(path) {
  return path === '/' ? 'language-entry' : path.slice(1).replace(/[^A-Za-z0-9_-]/g, '_');
}
