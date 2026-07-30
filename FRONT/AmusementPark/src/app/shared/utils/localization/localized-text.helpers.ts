import { LocalizedItem } from '@app/models/shared/localized-item';

export const DEFAULT_LOCALIZED_TEXT_FALLBACK: string = '—';

const GENERAL_LOCALIZED_LANGUAGE_CODES: readonly string[] = ['', '*', 'all', 'default', 'general', 'generic', 'neutral'];

export function stripHtml(value: string | null | undefined): string {
  if (!value) {
    return '';
  }

  return value
    .replace(/<[^>]*>/g, ' ')
    .replace(/&nbsp;/gi, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

export function isRichTextEmpty(value: string | null | undefined): boolean {
  return stripHtml(value).length === 0;
}

export function findExactLocalizedText(
  items: readonly LocalizedItem<string>[] | null | undefined,
  languageCode: string | null | undefined
): LocalizedItem<string> | undefined {
  if (!items || items.length === 0) {
    return undefined;
  }

  const normalizedLanguageCode: string = normalizeLanguageCode(languageCode);
  return items.find(
    (item: LocalizedItem<string>): boolean =>
      normalizeLanguageCode(item.languageCode) === normalizedLanguageCode
      && hasDisplayableRichTextContent(item.value)
  );
}

export function findLocalizedTextWithLanguage(
  items: readonly LocalizedItem<string>[] | null | undefined,
  languageCode: string | null | undefined,
  defaultLanguageCode: string = 'en'
): LocalizedItem<string> | undefined {
  if (!items || items.length === 0) {
    return undefined;
  }

  const exactMatch: LocalizedItem<string> | undefined =
    findExactLocalizedText(items, languageCode);
  if (exactMatch !== undefined) {
    return exactMatch;
  }

  const defaultMatch: LocalizedItem<string> | undefined =
    findExactLocalizedText(items, defaultLanguageCode);
  if (defaultMatch !== undefined) {
    return defaultMatch;
  }

  const generalMatch: LocalizedItem<string> | undefined = items.find(
    (item: LocalizedItem<string>): boolean =>
      isGeneralLanguageCode(item.languageCode) && hasDisplayableRichTextContent(item.value)
  );
  if (generalMatch !== undefined) {
    return generalMatch;
  }

  return items.find(
    (item: LocalizedItem<string>): boolean => hasDisplayableRichTextContent(item.value)
  );
}

export function resolveLocalizedValue<T>(
  items: readonly LocalizedItem<T>[] | null | undefined,
  languageCode: string | null | undefined,
  defaultLanguageCode: string = 'en'
): T | undefined {
  if (!items || items.length === 0) {
    return undefined;
  }

  const normalizedLanguageCode: string = normalizeLanguageCode(languageCode);
  const normalizedDefaultLanguageCode: string = normalizeLanguageCode(defaultLanguageCode);

  const exactMatch: LocalizedItem<T> | undefined = items.find(
    (item: LocalizedItem<T>) => normalizeLanguageCode(item.languageCode) === normalizedLanguageCode
  );

  if (exactMatch !== undefined) {
    return exactMatch.value;
  }

  const defaultMatch: LocalizedItem<T> | undefined = items.find(
    (item: LocalizedItem<T>) => normalizeLanguageCode(item.languageCode) === normalizedDefaultLanguageCode
  );

  if (defaultMatch !== undefined) {
    return defaultMatch.value;
  }

  const generalMatch: LocalizedItem<T> | undefined = items.find(
    (item: LocalizedItem<T>) => isGeneralLanguageCode(item.languageCode)
  );

  if (generalMatch !== undefined) {
    return generalMatch.value;
  }

  return items[0]?.value;
}

export function resolveLocalizedText(
  items: readonly LocalizedItem<string>[] | null | undefined,
  languageCode: string | null | undefined,
  fallback: string = DEFAULT_LOCALIZED_TEXT_FALLBACK,
  defaultLanguageCode: string = 'en'
): string {
  if (!items || items.length === 0) {
    return fallback;
  }

  const normalizedLanguageCode: string = normalizeLanguageCode(languageCode);
  const normalizedDefaultLanguageCode: string = normalizeLanguageCode(defaultLanguageCode);

  const exactMatch: LocalizedItem<string> | undefined = items.find(
    (item: LocalizedItem<string>) =>
      normalizeLanguageCode(item.languageCode) === normalizedLanguageCode && hasText(item.value)
  );

  if (exactMatch !== undefined) {
    return exactMatch.value;
  }

  const defaultMatch: LocalizedItem<string> | undefined = items.find(
    (item: LocalizedItem<string>) =>
      normalizeLanguageCode(item.languageCode) === normalizedDefaultLanguageCode && hasText(item.value)
  );

  if (defaultMatch !== undefined) {
    return defaultMatch.value;
  }

  const generalMatch: LocalizedItem<string> | undefined = items.find(
    (item: LocalizedItem<string>) => isGeneralLanguageCode(item.languageCode) && hasText(item.value)
  );

  if (generalMatch !== undefined) {
    return generalMatch.value;
  }

  const firstNonEmpty: LocalizedItem<string> | undefined = items.find(
    (item: LocalizedItem<string>) => hasText(item.value)
  );

  return firstNonEmpty?.value ?? fallback;
}

function normalizeLanguageCode(languageCode: string | null | undefined): string {
  return (languageCode ?? '').trim().toLowerCase();
}

function isGeneralLanguageCode(languageCode: string | null | undefined): boolean {
  return GENERAL_LOCALIZED_LANGUAGE_CODES.includes(normalizeLanguageCode(languageCode));
}

function hasText(value: string | null | undefined): boolean {
  return (value ?? '').trim().length > 0;
}

function hasDisplayableRichTextContent(value: string | null | undefined): boolean {
  return !isRichTextEmpty(value) || /<img\b/i.test(value ?? '');
}
