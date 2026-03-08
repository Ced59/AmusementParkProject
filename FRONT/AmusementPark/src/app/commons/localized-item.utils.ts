import { LocalizedItem } from '../models/shared/localized-item';

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

export function resolveLocalizedValue<T>(
  items: LocalizedItem<T>[] | null | undefined,
  languageCode: string,
  defaultLanguageCode: string = 'en'
): T | undefined {
  if (!items || items.length === 0) {
    return undefined;
  }

  const normalizedLanguageCode = languageCode.toLowerCase();
  const normalizedDefaultLanguageCode = defaultLanguageCode.toLowerCase();

  const exactMatch = items.find((item: LocalizedItem<T>) => item.languageCode.toLowerCase() === normalizedLanguageCode);

  if (exactMatch) {
    return exactMatch.value;
  }

  const defaultMatch = items.find((item: LocalizedItem<T>) => item.languageCode.toLowerCase() === normalizedDefaultLanguageCode);

  if (defaultMatch) {
    return defaultMatch.value;
  }

  return items[0]?.value;
}
