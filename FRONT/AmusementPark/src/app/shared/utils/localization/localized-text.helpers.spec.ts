import { LocalizedItem } from '@app/models/shared/localized-item';

import {
  DEFAULT_LOCALIZED_TEXT_FALLBACK,
  isRichTextEmpty,
  resolveLocalizedText,
  resolveLocalizedValue,
  stripHtml
} from './localized-text.helpers';

describe('localized-text helpers', () => {
  const localizedItems: LocalizedItem<string>[] = [
    { languageCode: 'fr', value: 'Bonjour' },
    { languageCode: 'en', value: 'Hello' },
    { languageCode: 'es', value: 'Hola' }
  ];

  it('resolves exact language matches case-insensitively', () => {
    expect(resolveLocalizedValue(localizedItems, ' FR ')).toBe('Bonjour');
  });

  it('falls back to the default language when the requested language is missing', () => {
    expect(resolveLocalizedValue(localizedItems, 'de')).toBe('Hello');
  });

  it('falls back to general localized values when exact and default languages are missing', () => {
    const values: LocalizedItem<string>[] = [
      { languageCode: 'general', value: 'Common label' },
      { languageCode: 'nl', value: 'Hallo' }
    ];

    expect(resolveLocalizedValue(values, 'de')).toBe('Common label');
  });

  it('falls back to the first item when the default language is missing', () => {
    expect(resolveLocalizedValue([{ languageCode: 'nl', value: 'Hallo' }], 'de', 'en')).toBe('Hallo');
  });

  it('returns undefined for empty localized collections', () => {
    expect(resolveLocalizedValue([], 'fr')).toBeUndefined();
    expect(resolveLocalizedValue(null, 'fr')).toBeUndefined();
  });

  it('ignores empty exact/default text values and returns first non empty text', () => {
    const values: LocalizedItem<string>[] = [
      { languageCode: 'fr', value: '   ' },
      { languageCode: 'en', value: '' },
      { languageCode: 'de', value: 'Hallo' }
    ];

    expect(resolveLocalizedText(values, 'fr')).toBe('Hallo');
  });

  it('uses non empty general text before unrelated localized text', () => {
    const values: LocalizedItem<string>[] = [
      { languageCode: 'fr', value: '   ' },
      { languageCode: 'general', value: 'Shared description' },
      { languageCode: 'de', value: 'Hallo' }
    ];

    expect(resolveLocalizedText(values, 'fr')).toBe('Shared description');
  });

  it('returns the configured fallback when no text is available', () => {
    expect(resolveLocalizedText(null, 'fr')).toBe(DEFAULT_LOCALIZED_TEXT_FALLBACK);
    expect(resolveLocalizedText([{ languageCode: 'fr', value: '' }], 'fr', 'N/A')).toBe('N/A');
  });

  it('strips rich html into plain compact text', () => {
    expect(stripHtml('<p>Hello&nbsp;<strong>world</strong></p>')).toBe('Hello world');
  });

  it('detects empty rich text values', () => {
    expect(isRichTextEmpty('<p>&nbsp;</p>')).toBeTrue();
    expect(isRichTextEmpty('<p>Visible</p>')).toBeFalse();
  });
});
