export function canRenderPublicDocument(path) {
  if (typeof path !== 'string' || path.length > 2048 || !path.startsWith('/') || path.startsWith('//') || path.includes('\\')) return false;
  const url = new URL(path, 'https://apf-review.invalid');
  return url.origin === 'https://apf-review.invalid' && url.pathname + url.search === path
    && /^\/(?:fr|en|de|nl|it|es|pt|pl)\/(?:park|attraction)\/[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\/[^?#/]+(?:\/[^?#]+)?(?:\?[^#]*)?$/i.test(path);
}
