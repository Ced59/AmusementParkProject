import { SOCIAL_PREVIEW_PATH_VERSION } from '../../app/shared/utils/images/social-preview-image.constants';

const FACEBOOK_APP_ID_PATTERN = /^\d+$/;
const FACEBOOK_IMAGE_ID_PATTERN = /^[A-Za-z0-9_-]{1,128}$/;
const CLOSING_HEAD_PATTERN = /<\/head\s*>/i;
export const FACEBOOK_IMAGE_QUERY_PARAMETER = 'facebook-image';

interface FacebookImageOverride {
  imageId: string;
  expectedOwnerType: 'PARK' | 'PARK_ITEM';
  expectedOwnerId: string;
  expectedCategory: 'PARK' | 'PARK_ITEM';
}

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

  return replaceOrInsertMeta(html, 'property', 'fb:app_id', normalizedAppId);
}

export function injectFacebookImageOverrideMeta(
  html: string,
  requestUrl: string,
  publicUrl: string,
): string {
  const override: FacebookImageOverride | null = resolveFacebookImageOverride(requestUrl);
  if (override === null) {
    return html;
  }

  let imageUrl: string;
  try {
    const parsedImageUrl: URL = new URL(
      `/api/images/binary/${encodeURIComponent(override.imageId)}/${SOCIAL_PREVIEW_PATH_VERSION}`,
      publicUrl,
    );
    parsedImageUrl.searchParams.set('expectedOwnerType', override.expectedOwnerType);
    parsedImageUrl.searchParams.set('expectedOwnerId', override.expectedOwnerId);
    parsedImageUrl.searchParams.set('expectedCategory', override.expectedCategory);
    imageUrl = parsedImageUrl.href;
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

export function hasFacebookImageOverrideQuery(url: string): boolean {
  return resolveFacebookImageOverride(url) !== null;
}

export function resolveFacebookOpenGraphAppId(
  value: string | null | undefined,
  facebookPublishingEnabled: string | null | undefined,
): string | null {
  const normalizedAppId: string | null = normalizeFacebookAppId(value);
  if (facebookPublishingEnabled?.trim().toLowerCase() === 'true' && normalizedAppId === null) {
    throw new Error(
      'FACEBOOK_APP_ID must be configured when SOCIAL_PUBLISHING_FACEBOOK_ENABLED is true.',
    );
  }

  return normalizedAppId;
}

export function hasOnlyFacebookImageOverrideQuery(url: string): boolean {
  if (resolveFacebookImageOverride(url) === null) {
    return false;
  }

  try {
    const parsedUrl: URL = new URL(url, 'https://amusement-parks.fun');
    return Array.from(parsedUrl.searchParams.keys()).length === 1;
  } catch {
    return false;
  }
}

export function normalizeFacebookImageOverrideCacheUrl(url: string): string {
  if (!hasOnlyFacebookImageOverrideQuery(url)) {
    return url;
  }

  const queryIndex: number = url.indexOf('?');
  return queryIndex < 0 ? url : url.slice(0, queryIndex);
}

function resolveFacebookImageOverride(requestUrl: string): FacebookImageOverride | null {
  try {
    const parsedUrl: URL = new URL(requestUrl, 'https://amusement-parks.fun');
    const imageIds: string[] = parsedUrl.searchParams.getAll(FACEBOOK_IMAGE_QUERY_PARAMETER);
    if (imageIds.length !== 1 || !FACEBOOK_IMAGE_ID_PATTERN.test(imageIds[0])) {
      return null;
    }

    const segments: string[] = parsedUrl.pathname
      .split('/')
      .filter((segment: string) => segment.length > 0)
      .map(decodeURIComponent);
    if (segments.length < 4
      || segments[1].toLowerCase() !== 'park'
      || !FACEBOOK_IMAGE_ID_PATTERN.test(segments[2])) {
      return null;
    }

    if (segments[4]?.toLowerCase() === 'item') {
      const itemId: string | undefined = segments[5];
      if (segments.length < 7 || itemId === undefined || !FACEBOOK_IMAGE_ID_PATTERN.test(itemId)) {
        return null;
      }

      return {
        imageId: imageIds[0],
        expectedOwnerType: 'PARK_ITEM',
        expectedOwnerId: itemId,
        expectedCategory: 'PARK_ITEM',
      };
    }

    return {
      imageId: imageIds[0],
      expectedOwnerType: 'PARK',
      expectedOwnerId: segments[2],
      expectedCategory: 'PARK',
    };
  } catch {
    return null;
  }
}

function replaceOrInsertMeta(
  html: string,
  attribute: 'name' | 'property',
  key: string,
  content: string,
): string {
  const pattern: RegExp = buildMetaPattern(attribute, key);
  const escapedContent: string = content.replace(/&/g, '&amp;').replace(/"/g, '&quot;');
  const metaTag: string = `<meta ${attribute}="${key}" content="${escapedContent}">`;
  if (pattern.test(html)) {
    pattern.lastIndex = 0;
    let replaced: boolean = false;
    return html.replace(pattern, (): string => {
      if (replaced) {
        return '';
      }

      replaced = true;
      return metaTag;
    });
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
  return new RegExp(`<meta\\s+[^>]*${attribute}=(['"])${escapedKey}\\1[^>]*>`, 'gi');
}
