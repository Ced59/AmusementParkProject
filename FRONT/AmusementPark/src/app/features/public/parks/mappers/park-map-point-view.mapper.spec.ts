import { ParkMapPoint } from '@app/models/parks/park-map-point';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';

import { mapParkMapPointToViewModel } from './park-map-point-view.mapper';

describe('mapParkMapPointToViewModel', () => {
  let countryDisplayService: jasmine.SpyObj<CountryDisplayService>;

  beforeEach(() => {
    countryDisplayService = jasmine.createSpyObj<CountryDisplayService>('CountryDisplayService', ['resolveLocalizedCountryName']);
    countryDisplayService.resolveLocalizedCountryName.and.returnValue('Belgium');
  });

  function createPoint(overrides: Partial<ParkMapPoint> = {}): ParkMapPoint {
    return {
      id: ' park-1 ',
      name: ' Test Park ',
      countryCode: ' be ',
      city: ' Brussels ',
      street: ' Street ',
      postalCode: '1000',
      latitude: 50.1234,
      longitude: 3.9876,
      currentLogoImageId: ' logo ',
      ...overrides
    };
  }

  it('maps and normalizes valid map points', () => {
    const result = mapParkMapPointToViewModel(createPoint(), 'fr', countryDisplayService);

    expect(result?.id).toBe('park-1');
    expect(result?.name).toBe('Test Park');
    expect(result?.countryCode).toBe('BE');
    expect(result?.countryName).toBe('Belgium');
    expect(result?.locationLine).toBe('Brussels · Belgium');
    expect(result?.addressLine).toBe('Street, 1000 Brussels');
    expect(result?.coordinatesLine).toBe('50.123, 3.988');
    expect(result?.logoImageId).toBe('logo');
  });

  it('falls back to country code when localized country name is missing', () => {
    countryDisplayService.resolveLocalizedCountryName.and.returnValue(null);

    expect(mapParkMapPointToViewModel(createPoint(), 'fr', countryDisplayService)?.locationLine).toBe('Brussels · BE');
  });

  it('returns null when id, name or coordinates are invalid', () => {
    expect(mapParkMapPointToViewModel(createPoint({ id: ' ' }), 'fr', countryDisplayService)).toBeNull();
    expect(mapParkMapPointToViewModel(createPoint({ name: ' ' }), 'fr', countryDisplayService)).toBeNull();
    expect(mapParkMapPointToViewModel(createPoint({ latitude: Number.NaN }), 'fr', countryDisplayService)).toBeNull();
    expect(mapParkMapPointToViewModel(createPoint({ longitude: Number.POSITIVE_INFINITY }), 'fr', countryDisplayService)).toBeNull();
  });
});
