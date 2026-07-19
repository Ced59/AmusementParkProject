import { TranslateService } from '@ngx-translate/core';

import { LocalizedPluralPipe } from './localized-plural.pipe';

describe('LocalizedPluralPipe', () => {
  it('selects singular and plural French labels', () => {
    const translateService: TranslateService = createTranslateService({
      'counts.shop.one': 'boutique',
      'counts.shop.other': 'boutiques'
    });
    const pipe: LocalizedPluralPipe = new LocalizedPluralPipe(translateService);

    expect(pipe.transform('counts.shop', 1, {}, 'fr')).toBe('boutique');
    expect(pipe.transform('counts.shop', 2, {}, 'fr')).toBe('boutiques');
  });

  it('supports plural categories used by Polish', () => {
    const translateService: TranslateService = createTranslateService({
      'counts.attraction.one': 'atrakcja',
      'counts.attraction.few': 'atrakcje',
      'counts.attraction.many': 'atrakcji',
      'counts.attraction.other': 'atrakcji'
    });
    const pipe: LocalizedPluralPipe = new LocalizedPluralPipe(translateService);

    expect(pipe.transform('counts.attraction', 1, {}, 'pl')).toBe('atrakcja');
    expect(pipe.transform('counts.attraction', 2, {}, 'pl')).toBe('atrakcje');
    expect(pipe.transform('counts.attraction', 5, {}, 'pl')).toBe('atrakcji');
  });

  it('falls back to the other form when a locale category is not configured', () => {
    const translateService: TranslateService = createTranslateService({
      'counts.service.other': 'services'
    });
    const pipe: LocalizedPluralPipe = new LocalizedPluralPipe(translateService);

    expect(pipe.transform('counts.service', 1, {}, 'fr')).toBe('services');
  });

  it('uses the configured Portuguese regional locale for route languages', () => {
    const translateService: TranslateService = createTranslateService({
      'counts.video.one': '{{count}} vídeo',
      'counts.video.other': '{{count}} vídeos'
    });
    const pipe: LocalizedPluralPipe = new LocalizedPluralPipe(translateService);

    expect(pipe.transform('counts.video', 0, {}, 'pt')).toBe('{{count}} vídeos');
  });
});

function createTranslateService(translations: Record<string, string>): TranslateService {
  return {
    currentLang: 'en',
    defaultLang: 'en',
    instant: (key: string): string => translations[key] ?? key
  } as TranslateService;
}
