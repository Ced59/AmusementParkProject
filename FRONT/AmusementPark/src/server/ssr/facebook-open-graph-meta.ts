const FACEBOOK_APP_ID_PATTERN = /^\d+$/;
const FACEBOOK_APP_ID_META_PATTERN = /<meta\s+[^>]*property=(['"])fb:app_id\1[^>]*>/i;
const CLOSING_HEAD_PATTERN = /<\/head\s*>/i;

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
