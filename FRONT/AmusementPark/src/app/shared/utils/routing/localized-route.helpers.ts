import { LANGUAGES, LanguageOption } from '@shared/models/localization';

const DEFAULT_LANGUAGE: string = 'en';

export function resolveSupportedLanguage(language: string | null | undefined, fallbackLanguage: string | null | undefined = DEFAULT_LANGUAGE): string {
  const normalizedLanguage: string = normalizeLanguage(language);

  if (isSupportedLanguage(normalizedLanguage)) {
    return normalizedLanguage;
  }

  const normalizedFallbackLanguage: string = normalizeLanguage(fallbackLanguage);

  if (isSupportedLanguage(normalizedFallbackLanguage)) {
    return normalizedFallbackLanguage;
  }

  return DEFAULT_LANGUAGE;
}

export function resolveSupportedLanguageFromUrl(url: string | null | undefined, fallbackLanguage: string | null | undefined = DEFAULT_LANGUAGE): string {
  const firstSegment: string | undefined = (url ?? '').split('?')[0]?.split('/').filter((segment: string) => segment.length > 0)[0];

  return resolveSupportedLanguage(firstSegment, fallbackLanguage);
}

export function isSupportedLanguage(language: string | null | undefined): boolean {
  const normalizedLanguage: string = normalizeLanguage(language);

  return LANGUAGES.some((languageOption: LanguageOption): boolean => languageOption.value === normalizedLanguage);
}

function normalizeLanguage(language: string | null | undefined): string {
  return (language ?? '').trim().toLowerCase();
}
