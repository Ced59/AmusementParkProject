import {
  buildPreferredLanguageHomeUrl,
  resolveLanguagePreferenceCookie
} from './language-preference-cookie';

describe('language preference cookie', () => {
  it('resolves a supported language from a cookie header', () => {
    expect(resolveLanguagePreferenceCookie('theme=dark; amusementpark.language=fr')).toBe('fr');
    expect(resolveLanguagePreferenceCookie(['theme=dark', 'amusementpark.language=PT'])).toBe('pt');
  });

  it('ignores missing, malformed, and unsupported preferences', () => {
    expect(resolveLanguagePreferenceCookie(undefined)).toBeNull();
    expect(resolveLanguagePreferenceCookie('amusementpark.language=ja')).toBeNull();
    expect(resolveLanguagePreferenceCookie('amusementpark.language=%E0%A4%A')).toBeNull();
  });

  it('preserves the neutral entry query string when redirecting', () => {
    expect(buildPreferredLanguageHomeUrl('de', '/?source=bookmark')).toBe('/de/home?source=bookmark');
  });
});
