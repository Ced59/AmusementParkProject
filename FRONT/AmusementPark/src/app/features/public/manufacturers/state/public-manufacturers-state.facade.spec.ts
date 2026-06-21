import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { PUBLIC_MANUFACTURERS_PORT, PublicManufacturersPort } from './public-manufacturers-state-data.ports';
import { PublicManufacturersStateFacade } from './public-manufacturers-state.facade';

describe('PublicManufacturersStateFacade', () => {
  let manufacturersPort: jasmine.SpyObj<PublicManufacturersPort>;
  let facade: PublicManufacturersStateFacade;

  beforeEach(() => {
    manufacturersPort = jasmine.createSpyObj<PublicManufacturersPort>('PublicManufacturersPort', ['getAllAttractionManufacturers']);

    TestBed.configureTestingModule({
      providers: [
        PublicManufacturersStateFacade,
        { provide: PUBLIC_MANUFACTURERS_PORT, useValue: manufacturersPort }
      ]
    });

    facade = TestBed.inject(PublicManufacturersStateFacade);
  });

  it('loads manufacturers sorted by name and grouped by first letter', () => {
    manufacturersPort.getAllAttractionManufacturers.and.returnValue(of([
      buildManufacturer({ name: 'Zierer' }),
      buildManufacturer({ name: 'Bolliger & Mabillard' }),
      buildManufacturer({ name: 'Intamin' })
    ]));

    facade.load();

    expect(facade.loading()).toBeFalse();
    expect(facade.filteredManufacturers().map((manufacturer: AttractionManufacturer) => manufacturer.name)).toEqual([
      'Bolliger & Mabillard',
      'Intamin',
      'Zierer'
    ]);
    expect(facade.groupedManufacturers().map((group) => group.letter)).toEqual(['B', 'I', 'Z']);
  });

  it('filters manufacturers by normalized text', () => {
    manufacturersPort.getAllAttractionManufacturers.and.returnValue(of([
      buildManufacturer({ name: 'Mack Rides', contactDetails: { city: 'Waldkirch', countryCode: 'DE' } }),
      buildManufacturer({ name: 'Vekoma', contactDetails: { city: 'Vlodrop', countryCode: 'NL' } })
    ]));

    facade.load();
    facade.updateSearchTerm('de');

    expect(facade.filteredManufacturers().map((manufacturer: AttractionManufacturer) => manufacturer.name)).toEqual(['Mack Rides']);
  });

  it('exposes an error key when loading fails', () => {
    manufacturersPort.getAllAttractionManufacturers.and.returnValue(throwError(() => new Error('network')));

    facade.load();

    expect(facade.loading()).toBeFalse();
    expect(facade.manufacturers()).toEqual([]);
    expect(facade.errorKey()).toBe('manufacturersPage.error');
  });
});

function buildManufacturer(overrides: Partial<AttractionManufacturer> = {}): AttractionManufacturer {
  return {
    id: overrides.name?.toLowerCase().replace(/[^a-z0-9]+/g, '-') ?? 'manufacturer',
    name: 'Manufacturer',
    biography: [],
    ...overrides
  };
}
