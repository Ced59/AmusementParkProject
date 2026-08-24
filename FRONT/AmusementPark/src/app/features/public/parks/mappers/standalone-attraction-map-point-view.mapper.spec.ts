import type { MockedObject } from 'vitest';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { StandaloneAttractionMapPoint } from '@app/models/standalone-attractions/standalone-attraction-map-point';
import { mapStandaloneAttractionMapPointToViewModel } from './standalone-attraction-map-point-view.mapper';

describe('mapStandaloneAttractionMapPointToViewModel', () => {
  it('maps a standalone attraction into a typed discovery point', () => {
    const countryDisplayService: MockedObject<CountryDisplayService> = {
      resolveLocalizedCountryName: vi.fn().mockReturnValue('Autriche')
    } as unknown as MockedObject<CountryDisplayService>;
    const point: StandaloneAttractionMapPoint = {
      id: ' standalone-1 ',
      name: ' Pendolino ',
      countryCode: 'at',
      type: 'RollerCoaster',
      subtype: 'Mountain Coaster',
      status: 'Operating',
      city: 'Nassfeld',
      street: null,
      postalCode: null,
      latitude: 46.561236,
      longitude: 13.253481
    };

    const result = mapStandaloneAttractionMapPointToViewModel(point, 'fr', countryDisplayService);

    expect(result).toMatchObject({
      kind: 'standaloneAttraction',
      id: 'standalone-1',
      name: 'Pendolino',
      countryCode: 'AT',
      countryName: 'Autriche',
      status: 'Operating',
      latitude: 46.561236,
      longitude: 13.253481
    });
  });
});
