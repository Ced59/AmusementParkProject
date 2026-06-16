import { resolveFlagAssetPath } from './flag-assets';

describe('resolveFlagAssetPath', () => {
  it('uses webp flags when the generated asset is smaller than the png source', () => {
    expect(resolveFlagAssetPath('en')).toBe('assets/flags/en.webp');
    expect(resolveFlagAssetPath('es')).toBe('assets/flags/es.webp');
    expect(resolveFlagAssetPath('pt')).toBe('assets/flags/pt.webp');
  });

  it('keeps png flags when the generated webp would be larger', () => {
    expect(resolveFlagAssetPath('fr')).toBe('assets/flags/fr.png');
    expect(resolveFlagAssetPath('de')).toBe('assets/flags/de.png');
  });

  it('normalizes language casing and surrounding whitespace', () => {
    expect(resolveFlagAssetPath(' EN ')).toBe('assets/flags/en.webp');
  });
});
