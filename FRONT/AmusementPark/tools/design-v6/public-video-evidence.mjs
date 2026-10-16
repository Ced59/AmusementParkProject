// Retain published identifiers/pagination only, never provider or account data.
export function publicVideoReadEvidence(target, status, bytes) {
  const url = new URL(target, 'https://amusement-parks.fun');
  const items = /^\/api\/videos\/parks\/[0-9a-f-]{36}\/park-items$/i.test(url.pathname);
  const park = url.pathname === '/api/videos' && url.searchParams.get('ownerType') === 'PARK'
    && /^[0-9a-f-]{36}$/i.test(url.searchParams.get('ownerId') ?? '');
  if ((!items && !park) || status !== 200) return undefined;
  if (url.searchParams.get('isPublished') === 'false') throw new Error('Unpublished video evidence is prohibited.');
  if (bytes.byteLength > 2 * 1024 * 1024) throw new Error('Public video evidence exceeds its response bound.');
  const payload = JSON.parse(Buffer.from(bytes).toString('utf8'));
  const pagination = payload.pagination;
  if (!Array.isArray(payload.data) || payload.data.length > 100 || !pagination
    || !['currentPage', 'itemsPerPage', 'totalItems'].every(key => Number.isInteger(pagination[key]))
    || pagination.currentPage < 1 || pagination.itemsPerPage < 1 || pagination.itemsPerPage > 100
    || pagination.itemsPerPage < payload.data.length || pagination.totalItems < payload.data.length) throw new Error('Invalid public video pagination.');
  const videos = payload.data.map(value => {
    if (items && (!value.item || value.item.isVisible === false)) throw new Error('A hidden item cannot enter public video evidence.');
    return items ? value.video : value;
  });
  if (videos.some(video => video?.isPublished !== true || !/^(?:[0-9a-f]{32}|[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12})$/i.test(video.id ?? ''))) {
    throw new Error('Invalid or unpublished video evidence.');
  }
  return { kind: items ? 'items' : 'park', ids: videos.map(video => video.id),
    pagination: { currentPage: pagination.currentPage, itemsPerPage: pagination.itemsPerPage, totalItems: pagination.totalItems } };
}
