// Keep public image identifiers and pagination only, never full media/EXIF DTOs.
export function publicPhotoReadEvidence(url, status, bytes) {
  const target = new URL(url, 'https://amusement-parks.fun');
  const items = /^\/api\/images\/parks\/[0-9a-f-]{36}\/park-items$/i.test(target.pathname);
  const park = /^\/api\/images\/1\/[0-9a-f-]{36}\/[12]$/i.test(target.pathname);
  if ((!items && !park) || status !== 200) return undefined;
  if (target.searchParams.has('includeUnpublished') && target.searchParams.get('includeUnpublished') !== 'false') throw new Error('Unpublished photo evidence is prohibited.');
  if (bytes.byteLength > 2 * 1024 * 1024) throw new Error('Public photo evidence exceeds its bounded payload.');
  const payload = JSON.parse(Buffer.from(bytes).toString('utf8'));
  const pagination = payload.pagination;
  if (!Array.isArray(payload.data) || payload.data.length > 100 || !pagination
    || !['currentPage', 'itemsPerPage', 'totalItems'].every(key => Number.isInteger(pagination[key]))
    || pagination.currentPage < 1 || pagination.itemsPerPage < 1 || pagination.itemsPerPage > 100
    || pagination.itemsPerPage < payload.data.length || pagination.totalItems < payload.data.length) throw new Error('Invalid public photo pagination.');
  const images = payload.data.map(value => {
    // The anonymous endpoint filters public items server-side; its compact
    // ParkItemDto omits isVisible. Never accept an explicit hidden flag.
    if (items && (!value.item || value.item.isVisible === false)) throw new Error('A hidden item cannot enter public photo evidence.');
    return items ? value.image : value;
  });
  if (images.some(image => image?.isPublished !== true || !/^(?:[0-9a-f]{32}|[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12})$/i.test(image.id ?? ''))) throw new Error('Invalid or unpublished photo evidence.');
  return { kind: items ? 'items' : 'park', ids: images.map(image => image.id),
    pagination: { currentPage: pagination.currentPage, itemsPerPage: pagination.itemsPerPage, totalItems: pagination.totalItems } };
}
