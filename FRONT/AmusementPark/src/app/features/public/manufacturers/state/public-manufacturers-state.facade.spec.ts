import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';

import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PagedResult } from '@shared/models/contracts';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { createPagedResult } from '@shared/utils/mapping';
import {
  PUBLIC_MANUFACTURERS_PORT,
  PublicManufacturersPort,
} from './public-manufacturers-state-data.ports';
import { PublicManufacturersStateFacade } from './public-manufacturers-state.facade';

describe('PublicManufacturersStateFacade', () => {
  let manufacturersPort: MockedObject<PublicManufacturersPort>;
  const httpStatus = { setNotFound: vi.fn(), setStatus: vi.fn() };
  let facade: PublicManufacturersStateFacade;

  beforeEach(() => {
    vi.clearAllMocks();
    manufacturersPort = {
      getAttractionManufacturersPage: vi
        .fn()
        .mockName('PublicManufacturersPort.getAttractionManufacturersPage'),
      getAllAttractionManufacturers: vi
        .fn()
        .mockName('PublicManufacturersPort.getAllAttractionManufacturers'),
    } as unknown as MockedObject<PublicManufacturersPort>;

    TestBed.configureTestingModule({
      providers: [
        PublicManufacturersStateFacade,
        { provide: SsrHttpStatusService, useValue: httpStatus },
        { provide: PUBLIC_MANUFACTURERS_PORT, useValue: manufacturersPort },
      ],
    });

    facade = TestBed.inject(PublicManufacturersStateFacade);
  });

  it('loads a manufacturer page sorted by name and grouped by first letter', () => {
    manufacturersPort.getAttractionManufacturersPage.mockReturnValue(
      of(
        createPagedResult(
          [
            buildManufacturer({ name: 'Zierer' }),
            buildManufacturer({ name: 'Bolliger & Mabillard' }),
            buildManufacturer({ name: 'Intamin' }),
          ],
          { currentPage: 1, itemsPerPage: 24, totalItems: 3, totalPages: 1 },
        ),
      ),
    );

    facade.load();

    expect(
      manufacturersPort.getAttractionManufacturersPage,
    ).toHaveBeenCalledWith(1, 24, '');
    expect(facade.loading()).toBe(false);
    expect(facade.totalCount()).toBe(3);
    expect(
      facade
        .filteredManufacturers()
        .map((manufacturer: AttractionManufacturer) => manufacturer.name),
    ).toEqual(['Bolliger & Mabillard', 'Intamin', 'Zierer']);
    expect(facade.groupedManufacturers().map((group) => group.letter)).toEqual([
      'B',
      'I',
      'Z',
    ]);
  });

  it('loads the requested page with the current search term', () => {
    manufacturersPort.getAttractionManufacturersPage.mockReturnValue(
      of(
        createPagedResult([buildManufacturer({ name: 'Mack Rides' })], {
          currentPage: 2,
          itemsPerPage: 12,
          totalItems: 13,
          totalPages: 2,
        }),
      ),
    );

    facade.updateSearchTerm(' ride ');
    facade.setPage(2, 12);

    expect(
      manufacturersPort.getAttractionManufacturersPage,
    ).toHaveBeenCalledWith(2, 12, 'ride');
    expect(facade.searchTerm()).toBe('ride');
    expect(facade.currentPage()).toBe(2);
    expect(facade.pageSize()).toBe(12);
    expect(facade.totalCount()).toBe(13);
  });

  it('exposes an error key when loading fails', () => {
    manufacturersPort.getAttractionManufacturersPage.mockReturnValue(
      throwError(() => new Error('network')),
    );

    facade.load();

    expect(facade.loading()).toBe(false);
    expect(facade.manufacturers()).toEqual([]);
    expect(facade.pagination()).toBeNull();
    expect(facade.errorKey()).toBe('manufacturersPage.error');
    expect(facade.resolvedPage()).toBeNull();
    expect(httpStatus.setStatus).toHaveBeenCalledWith(503);
  });

  it('accepts the one-card final page using only its bounded page request', () => {
    manufacturersPort.getAttractionManufacturersPage.mockReturnValue(of(createPagedResult([buildManufacturer()], {
      currentPage: 2, itemsPerPage: 24, totalItems: 25, totalPages: 2
    })));
    facade.load(2);
    expect(facade.resolvedPage()).toBe(2);
    expect(facade.manufacturers()).toHaveLength(1);
    expect(httpStatus.setNotFound).not.toHaveBeenCalled();
    expect(manufacturersPort.getAllAttractionManufacturers).not.toHaveBeenCalled();
  });

  it('rejects a malformed page without making an API request', () => {
    facade.load(0);
    expect(manufacturersPort.getAttractionManufacturersPage).not.toHaveBeenCalled();
    expect(httpStatus.setNotFound).toHaveBeenCalledOnce();
    expect(facade.resolvedPage()).toBeNull();
  });

  it('marks a page beyond the known total as not found', () => {
    manufacturersPort.getAttractionManufacturersPage.mockReturnValue(of(createPagedResult([], {
      currentPage: 3, itemsPerPage: 24, totalItems: 25, totalPages: 2
    })));
    facade.load(3);
    expect(httpStatus.setNotFound).toHaveBeenCalledOnce();
    expect(facade.resolvedPage()).toBeNull();
  });

  it.each([
    { currentPage: 1, itemsPerPage: 24, totalItems: 25, totalPages: 2 },
    { currentPage: 2, itemsPerPage: 12, totalItems: 25, totalPages: 3 },
    { currentPage: 2, itemsPerPage: 24, totalItems: 25, totalPages: 9 },
    { currentPage: 2, itemsPerPage: 24, totalItems: 26, totalPages: 2 }
  ])('does not index a mismatched API page: %j', pagination => {
    manufacturersPort.getAttractionManufacturersPage.mockReturnValue(of(createPagedResult([buildManufacturer()], pagination)));
    facade.load(2);
    expect(httpStatus.setStatus).toHaveBeenCalledWith(503);
    expect(facade.resolvedPage()).toBeNull();
    expect(facade.manufacturers()).toEqual([]);
  });

  it.each([{ id: '' }, { name: ' ' }])('does not mistake non-routable data for a useful page: %j', overrides => {
    manufacturersPort.getAttractionManufacturersPage.mockReturnValue(of(createPagedResult([buildManufacturer(overrides)], {
      currentPage: 1, itemsPerPage: 24, totalItems: 1, totalPages: 1
    })));
    facade.load();
    expect(httpStatus.setStatus).toHaveBeenCalledWith(503);
    expect(facade.resolvedPage()).toBeNull();
  });

  it('does not mark an empty interactive search as a missing public page', () => {
    manufacturersPort.getAttractionManufacturersPage.mockReturnValue(of(createPagedResult([], {
      currentPage: 1, itemsPerPage: 24, totalItems: 0, totalPages: 0
    })));
    facade.updateSearchTerm('missing');
    facade.load();
    expect(facade.errorKey()).toBeNull();
    expect(facade.resolvedPage()).toBeNull();
    expect(httpStatus.setStatus).not.toHaveBeenCalled();
    expect(httpStatus.setNotFound).not.toHaveBeenCalled();
  });

  it('clears validated SEO immediately and ignores a stale response when search changes', () => {
    const pending = new Subject<PagedResult<AttractionManufacturer>>();
    manufacturersPort.getAttractionManufacturersPage.mockReturnValue(pending);
    facade.load(2);
    facade.updateSearchTerm('ride');
    pending.next(createPagedResult([buildManufacturer()], {
      currentPage: 2, itemsPerPage: 24, totalItems: 25, totalPages: 2
    }));
    expect(facade.resolvedPage()).toBeNull();
    expect(facade.manufacturers()).toEqual([]);
  });

});

function buildManufacturer(
  overrides: Partial<AttractionManufacturer> = {},
): AttractionManufacturer {
  return {
    id:
      overrides.name?.toLowerCase().replace(/[^a-z0-9]+/g, '-') ??
      'manufacturer',
    name: 'Manufacturer',
    biography: [],
    ...overrides,
  };
}
