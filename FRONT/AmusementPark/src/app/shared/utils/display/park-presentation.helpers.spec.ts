import { Park } from '@app/models/parks/park';

import { buildEntitySlug, buildParkAddressLine, buildParkLocationLine, buildParkSlug } from './park-presentation.helpers';

describe('park-presentation helpers', () => {
  it('builds slugs by lowercasing, removing accents and compacting separators', () => {
    expect(buildEntitySlug('  Fête à Bobbejaanland! 2026 ')).toBe('fete-a-bobbejaanland-2026');
    expect(buildParkSlug('Parc Astérix')).toBe('parc-asterix');
  });

  it('returns an empty slug for nullish or punctuation-only values', () => {
    expect(buildEntitySlug(null)).toBe('');
    expect(buildEntitySlug(' --- ')).toBe('');
  });

  it('uses explicit fallback slugs when normalized content is empty', () => {
    expect(buildParkSlug('東京')).toBe('park');
    expect(buildEntitySlug('東京', 'item')).toBe('item');
  });

  it('builds location lines with an override country name when provided', () => {
    const park: Park = { name: 'Test', city: 'Brühl', countryCode: 'DE', latitude: 0, longitude: 0 };

    expect(buildParkLocationLine(park, 'Germany')).toBe('Brühl · Germany');
  });

  it('falls back to country code and skips blank location parts', () => {
    const park: Park = { name: 'Test', city: ' ', countryCode: 'BE', latitude: 0, longitude: 0 };

    expect(buildParkLocationLine(park)).toBe('BE');
    expect(buildParkLocationLine(undefined)).toBeNull();
  });

  it('builds address lines from non-empty address parts', () => {
    const park: Park = { name: 'Test', street: 'Rue du Parc', postalCode: '75000', city: 'Paris', latitude: 0, longitude: 0 };

    expect(buildParkAddressLine(park)).toBe('Rue du Parc, 75000, Paris');
  });

  it('returns null for empty addresses', () => {
    const park: Park = { name: 'Test', street: ' ', latitude: 0, longitude: 0 };

    expect(buildParkAddressLine(park)).toBeNull();
  });
});
