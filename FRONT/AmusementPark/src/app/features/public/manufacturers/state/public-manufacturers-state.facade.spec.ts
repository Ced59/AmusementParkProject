import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { createPagedResult } from '@shared/utils/mapping';
import { PUBLIC_MANUFACTURERS_PORT, PublicManufacturersPort } from './public-manufacturers-state-data.ports';
import { PublicManufacturersStateFacade } from './public-manufacturers-state.facade';

describe('PublicManufacturersStateFacade', () => {
  let manufacturersPort: jasmine.SpyObj<PublicManufacturersPort>;
  let facade: PublicManufacturersStateFacade;

  beforeEach(() => {
    manufacturersPort = jasmine.createSpyObj<PublicManufacturersPort>('PublicManufacturersPort', [
      'getAttractionManufacturersPage',
      'getAllAttractionManufacturers'
    ]);

    TestBed.configureTestingModule({
      providers: [
        PublicManufacturersStateFacade,
        { provide: PUBLIC_MANUFACTURERS_PORT, useValue: manufacturersPort }
      ]
    });

    facade = TestBed.inject(PublicManufacturersStateFacade);
  });

  it('loads a manufacturer page sorted by name and grouped by first letter', () => {
    manufacturersPort.getAttractionManufacturersPage.and.returnValue(of(createPagedResult([
      buildManufacturer({ name: 'Zierer' }),
      buildManufacturer({ name: 'Bolliger & Mabillard' }),
      buildManufacturer({ name: 'Intamin' })
    ], { currentPage: 1, itemsPerPage: 24, totalItems: 30, totalPages: 2 })));

    facade.load();

    expect(manufacturersPort.getAttractionManufacturersPage).toHaveBeenCalledWith(1, 24, '');
    expect(facade.loading()).toBeFalse();
    expect(facade.totalCount()).toBe(30);
    expect(facade.filteredManufacturers().map((manufacturer: AttractionManufacturer) => manufacturer.name)).toEqual([
      'Bolliger & Mabillard',
      'Intamin',
      'Zierer'
    ]);
    expect(facade.groupedManufacturers().map((group) => group.letter)).toEqual(['B', 'I', 'Z']);
  });

  it('loads the requested page with the current search term', () => {
    manufacturersPort.getAttractionManufacturersPage.and.returnValue(of(createPagedResult([
      buildManufacturer({ name: 'Mack Rides' })
    ], { currentPage: 2, itemsPerPage: 12, totalItems: 13, totalPages: 2 })));

    facade.updateSearchTerm(' ride ');
    facade.setPage(2, 12);

    expect(manufacturersPort.getAttractionManufacturersPage).toHaveBeenCalledWith(2, 12, 'ride');
    expect(facade.searchTerm()).toBe('ride');
    expect(facade.currentPage()).toBe(2);
    expect(facade.pageSize()).toBe(12);
    expect(facade.totalCount()).toBe(13);
  });

  it('exposes an error key when loading fails', () => {
    manufacturersPort.getAttractionManufacturersPage.and.returnValue(throwError(() => new Error('network')));

    facade.load();

    expect(facade.loading()).toBeFalse();
    expect(facade.manufacturers()).toEqual([]);
    expect(facade.pagination()).toBeNull();
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
