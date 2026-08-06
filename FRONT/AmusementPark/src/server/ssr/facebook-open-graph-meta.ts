const FACEBOOK_APP_ID_PATTERN = /^\d+$/;
const FACEBOOK_IMAGE_ID_PATTERN = /^[A-Za-z0-9_-]{1,128}$/;
const FACEBOOK_APP_ID_META_PATTERN = /<meta\s+[^>]*property=(['"])fb:app_id\1[^>]*>/i;
const CLOSING_HEAD_PATTERN = /<\/head\s*>/i;
export const FACEBOOK_IMAGE_QUERY_PARAMETER = 'facebook-image';

export function normalizeFacebookAppId(value: string | null | undefined): string | null {
  const normalized: string = value?.trim() ?? '';

  return FACEBOOK_APP_ID_PATTERN.test(normalized) ? normalized : null;
}

export function injectFacebookAppIdMeta(
  html: string,
  appId: string | null | undefined,
): string {
  const normalizedAppId: string | null = normalizeFacebookAppId(appId);
  if (normalizedAppId === null) {
    return html;
  }

  const metaTag: string = `<meta property="fb:app_id" content="${normalizedAppId}">`;
  if (FACEBOOK_APP_ID_META_PATTERN.test(html)) {
    return html.replace(FACEBOOK_APP_ID_META_PATTERN, metaTag);
  }

  const closingHeadMatch: RegExpExecArray | null = CLOSING_HEAD_PATTERN.exec(html);
  if (closingHeadMatch === null) {
    return html;
  }

  return `${html.slice(0, closingHeadMatch.index)}${metaTag}\n${html.slice(closingHeadMatch.index)}`;
}

export function injectFacebookImageOverrideMeta(
  html: string,
  requestUrl: string,
  publicUrl: string,
): string {
  const imageId: string | null = resolveFacebookImageOverrideId(requestUrl);
  if (imageId === null) {
    return html;
  }

  let imageUrl: string;
  try {
    imageUrl = new URL(
      `/api/images/binary/${encodeURIComponent(imageId)}/social-preview-v1`,
      publicUrl,
    ).href;
  } catch {
    return html;
  }

  let result: string = replaceOrInsertMeta(html, 'property', 'og:image', imageUrl);
  result = replaceOrInsertMeta(result, 'property', 'og:image:secure_url', imageUrl);
  result = replaceOrInsertMeta(result, 'property', 'og:image:type', 'image/jpeg');
  result = removeMeta(result, 'property', 'og:image:width');
  result = removeMeta(result, 'property', 'og:image:height');
  result = replaceOrInsertMeta(result, 'name', 'twitter:image', imageUrl);
  return result;
}

export function hasOnlyFacebookImageOverrideQuery(url: string): boolean {
  const queryIndex: number = url.indexOf('?');
  if (queryIndex < 0) {
    return false;
  }

  try {
    const parsedUrl: URL = new URL(url, 'https://amusement-parks.fun');
    const entries: Array<[string, string]> = Array.from(parsedUrl.searchParams.entries());
    return entries.length === 1
      && entries[0][0] === FACEBOOK_IMAGE_QUERY_PARAMETER
      && FACEBOOK_IMAGE_ID_PATTERN.test(entries[0][1]);
  } catch {
    return false;
  }
}

function resolveFacebookImageOverrideId(requestUrl: string): string | null {
  if (!hasOnlyFacebookImageOverrideQuery(requestUrl)) {
    return null;
  }

  const parsedUrl: URL = new URL(requestUrl, 'https://amusement-parks.fun');
  return parsedUrl.searchParams.get(FACEBOOK_IMAGE_QUERY_PARAMETER);
}

function replaceOrInsertMeta(
  html: string,
  attribute: 'name' | 'property',
  key: string,
  content: string,
): string {
  const pattern: RegExp = buildMetaPattern(attribute, key);
  const metaTag: string = `<meta ${attribute}="${key}" content="${content}">`;
  if (pattern.test(html)) {
    return html.replace(pattern, metaTag);
  }

  const closingHeadMatch: RegExpExecArray | null = CLOSING_HEAD_PATTERN.exec(html);
  if (closingHeadMatch === null) {
    return html;
  }

  return `${html.slice(0, closingHeadMatch.index)}${metaTag}\n${html.slice(closingHeadMatch.index)}`;
}

function removeMeta(html: string, attribute: 'name' | 'property', key: string): string {
  return html.replace(buildMetaPattern(attribute, key), '');
}

function buildMetaPattern(attribute: 'name' | 'property', key: string): RegExp {
  const escapedKey: string = key.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  return new RegExp(`<meta\\s+[^>]*${attribute}=(['"])${escapedKey}\\1[^>]*>`, 'i');
}
