import {
  getLocalizedBooleanDisplay,
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  getParkTypeTranslationKey,
  getSearchCategoryTranslationKey,
  normalizeTranslationSegment
} from './display-label.helpers';

describe('display-label helpers', () => {
  it('builds translation keys with normalized enum segments', () => {
    expect(getParkTypeTranslationKey('ThemePark')).toBe('admin.parks.types.themePark');
    expect(getParkItemCategoryTranslationKey('Attraction')).toBe('parkExplorer.categories.attraction');
    expect(getParkItemTypeTranslationKey('RollerCoaster')).toBe('parkExplorer.types.rollerCoaster');
  });

  it('uses fallback segments for null or blank values', () => {
    expect(getParkTypeTranslationKey(null)).toBe('admin.parks.types.notSpecified');
    expect(getParkItemCategoryTranslationKey('   ')).toBe('parkExplorer.categories.other');
    expect(getParkItemTypeTranslationKey(undefined)).toBe('parkExplorer.types.other');
  });

  it('maps known search category plural names to public translation keys', () => {
    expect(getSearchCategoryTranslationKey('Parks')).toBe('home.categories.park');
    expect(getSearchCategoryTranslationKey('Park Items')).toBe('home.categories.parkItems');
    expect(getSearchCategoryTranslationKey('Manufacturers')).toBe('home.categories.manufacturers');
  });

  it('falls back to a normalized search category key for unknown categories', () => {
    expect(getSearchCategoryTranslationKey('Custom Category')).toBe('home.categories.customcategory');
    expect(getSearchCategoryTranslationKey(null)).toBe('home.categories.park');
  });

  it('returns localized boolean labels and null for unknown values', () => {
    expect(getLocalizedBooleanDisplay(true, 'fr')).toBe('Oui');
    expect(getLocalizedBooleanDisplay(false, 'fr')).toBe('Non');
    expect(getLocalizedBooleanDisplay(true, 'en')).toBe('Yes');
    expect(getLocalizedBooleanDisplay(false, 'de')).toBe('No');
    expect(getLocalizedBooleanDisplay(null, 'fr')).toBeNull();
  });

  it('normalizes only the first character of translation segments', () => {
    expect(normalizeTranslationSegment(' Roller Coaster ', 'fallback')).toBe('roller Coaster');
    expect(normalizeTranslationSegment('', 'fallback')).toBe('fallback');
  });
});
