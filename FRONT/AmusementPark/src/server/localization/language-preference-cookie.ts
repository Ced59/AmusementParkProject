import {
  LANGUAGES,
  LANGUAGE_PREFERENCE_COOKIE_NAME,
  LanguageOption
} from '../../app/shared/models/localization';

const supportedLanguages: ReadonlySet<string> = new Set<string>(
  LANGUAGES.map((language: LanguageOption): string => language.value)
);

export function resolveLanguagePreferenceCookie(cookieHeader: string | string[] | undefined): string | null {
  const headerValue: string = Array.isArray(cookieHeader) ? cookieHeader.join(';') : cookieHeader ?? '';
  const cookiePrefix: string = `${LANGUAGE_PREFERENCE_COOKIE_NAME}=`;
  const encodedValue: string | undefined = headerValue
    .split(';')
    .map((entry: string): string => entry.trim())
    .find((entry: string): boolean => entry.startsWith(cookiePrefix))
    ?.slice(cookiePrefix.length);

  if (!encodedValue) {
    return null;
  }

  try {
    const language: string = decodeURIComponent(encodedValue).trim().toLowerCase();
    return supportedLanguages.has(language) ? language : null;
  } catch (_error) {
    return null;
  }
}

export function buildPreferredLanguageHomeUrl(language: string, originalUrl: string): string {
  const queryIndex: number = originalUrl.indexOf('?');
  const query: string = queryIndex >= 0 ? originalUrl.slice(queryIndex) : '';
  return `/${language}/home${query}`;
}
