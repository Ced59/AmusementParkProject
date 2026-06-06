import { CountryDto } from '@app/models/countries/country-dto';

import { CountryDisplayService } from './country-display.service';

describe('CountryDisplayService', () => {
  let service: CountryDisplayService;

  beforeEach(() => {
    service = new CountryDisplayService();
  });

  it('returns null for invalid country codes', () => {
    expect(service.resolveLocalizedCountryName(null, 'fr')).toBeNull();
    expect(service.resolveLocalizedCountryName('BEL', 'fr')).toBeNull();
  });

  it('resolves localized country names when Intl.DisplayNames is available', () => {
    const result: string | null = service.resolveLocalizedCountryName('FR', 'en');

    expect(result).toBe('France');
  });

  it('falls back to normalized code when locale cannot be created', () => {
    const result: string | null = service.resolveLocalizedCountryName('be', 'invalid-locale-!');

    expect(result).toBe('BE');
  });

  it('resolves country codes from direct ISO codes in a known collection', () => {
    const countries: CountryDto[] = [{ isoCode: 'BE', name: 'Belgique' }];

    expect(service.resolveCountryCodeFromLocalizedName(' be ', countries)).toBe('BE');
  });

  it('resolves country codes from accent-insensitive localized names', () => {
    const countries: CountryDto[] = [
      { isoCode: 'BE', name: 'Belgique' },
      { isoCode: 'DE', name: 'Allemagne' }
    ];

    expect(service.resolveCountryCodeFromLocalizedName(' allemagne ', countries)).toBe('DE');
  });

  it('returns null when no country matches', () => {
    expect(service.resolveCountryCodeFromLocalizedName('Unknown', [{ isoCode: 'FR', name: 'France' }])).toBeNull();
  });

  it('checks country name matches accent-insensitively', () => {
    expect(service.countryNameMatches('Côte d’Ivoire', 'cote')).toBeTrue();
    expect(service.countryNameMatches('Belgique', 'gique')).toBeTrue();
    expect(service.countryNameMatches('Belgique', '')).toBeFalse();
  });
});
