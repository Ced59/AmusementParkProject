import { isSupportedLanguage, resolveSupportedLanguage, resolveSupportedLanguageFromUrl } from './localized-route.helpers';

describe('localized-route helpers', () => {
  it('resolves supported languages after trimming and lowercasing', () => {
    expect(resolveSupportedLanguage(' FR ')).toBe('fr');
    expect(resolveSupportedLanguage('PT')).toBe('pt');
  });

  it('uses a supported fallback before defaulting to English', () => {
    expect(resolveSupportedLanguage('xx', 'de')).toBe('de');
    expect(resolveSupportedLanguage('xx', 'yy')).toBe('en');
  });

  it('extracts the first path segment from URLs without query or hash', () => {
    expect(resolveSupportedLanguageFromUrl('/fr/parks?page=2#top')).toBe('fr');
    expect(resolveSupportedLanguageFromUrl('nl/park/1/name')).toBe('nl');
  });

  it('falls back when the URL does not start with a supported language', () => {
    expect(resolveSupportedLanguageFromUrl('/admin', 'it')).toBe('it');
    expect(resolveSupportedLanguageFromUrl(null, 'unknown')).toBe('en');
  });

  it('checks supported language codes strictly after normalization', () => {
    expect(isSupportedLanguage('es')).toBeTrue();
    expect(isSupportedLanguage(' es ')).toBeTrue();
    expect(isSupportedLanguage('es-ES')).toBeFalse();
    expect(isSupportedLanguage(null)).toBeFalse();
  });
});
