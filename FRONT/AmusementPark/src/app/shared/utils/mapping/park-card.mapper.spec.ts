import { Park } from '@app/models/parks/park';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';

import { mapParkToCardModel } from './park-card.mapper';

describe('mapParkToCardModel', () => {
  function createPark(overrides: Partial<Park> = {}): Park {
    return {
      id: 'park-1',
      name: ' Test Park ',
      countryCode: ' be ',
      city: ' Brussels ',
      street: ' Street ',
      postalCode: '1000',
      latitude: 50.12345,
      longitude: 3.98765,
      currentLogoImageId: ' logo-1 ',
      webSiteUrl: ' https://park.test ',
      descriptions: [{ languageCode: 'en', value: '<p>Hello <strong>world</strong></p>' }],
      ...overrides
    };
  }

  it('maps public card fields with localized country names and compact description', () => {
    const countryDisplayService: jasmine.SpyObj<CountryDisplayService> = jasmine.createSpyObj<CountryDisplayService>('CountryDisplayService', ['resolveLocalizedCountryName']);
    countryDisplayService.resolveLocalizedCountryName.and.returnValue('Belgium');

    const result = mapParkToCardModel(createPark(), 'en', countryDisplayService);

    expect(result.id).toBe('park-1');
    expect(result.name).toBe('Test Park');
    expect(result.countryCode).toBe('be');
    expect(result.locationLine).toBe('Brussels · Belgium');
    expect(result.addressLine).toBe('Street, 1000, Brussels');
    expect(result.coordinatesLine).toBe('50.123, 3.988');
    expect(result.shortDescription).toBe('Hello world');
  });

  it('returns null coordinate fields when coordinates are not finite', () => {
    const result = mapParkToCardModel(createPark({ latitude: Number.NaN, longitude: Number.POSITIVE_INFINITY }), 'en');

    expect(result.latitude).toBeNull();
    expect(result.longitude).toBeNull();
    expect(result.coordinatesLine).toBeNull();
  });

  it('truncates long descriptions and returns null for empty descriptions', () => {
    const longDescription: string = 'a'.repeat(200);
    const textTruncator: NaturalTextTruncatorService = new NaturalTextTruncatorService();

    expect(mapParkToCardModel(createPark({ descriptions: [{ languageCode: 'en', value: longDescription }] }), 'en', null, textTruncator).shortDescription)
      .toBe(`${'a'.repeat(137)}...`);
    expect(mapParkToCardModel(createPark({ descriptions: [] }), 'en').shortDescription).toBeNull();
  });
});
