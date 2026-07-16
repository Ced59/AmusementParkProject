import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { StandaloneAttraction, StandaloneAttractionMigrationRequest } from '@app/models/standalone-attractions/standalone-attraction';
import { ParkAdminListFilters, ParkAdminListSort } from '@data-access/parks/parks-api-endpoints';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { StandaloneAttractionsApiService } from '@data-access/standalone-attractions/standalone-attractions-api.service';
import { PagedResult, PaginationContract } from '@shared/models/contracts';

import { AdminStandaloneAttractionsComponent } from './admin-standalone-attractions.component';

class FakeStandaloneAttractionsApiService {
  public pageResponse$: Observable<PagedResult<StandaloneAttraction>> = of({
    items: [],
    pagination: createPagination()
  });
  public migrateResponse$: Observable<StandaloneAttraction> = of(createAttraction('standalone-1'));
  public readonly migrationCalls: StandaloneAttractionMigrationRequest[] = [];

  getPage(): Observable<PagedResult<StandaloneAttraction>> {
    return this.pageResponse$;
  }

  create(attraction: StandaloneAttraction): Observable<StandaloneAttraction> {
    return of({ ...attraction, id: 'created-standalone' });
  }

  update(_id: string, attraction: StandaloneAttraction): Observable<StandaloneAttraction> {
    return of(attraction);
  }

  updateBulkAdministration(): Observable<{ requestedCount: number; updatedCount: number }> {
    return of({ requestedCount: 0, updatedCount: 0 });
  }

  migrateFromPark(request: StandaloneAttractionMigrationRequest): Observable<StandaloneAttraction> {
    this.migrationCalls.push(request);
    return this.migrateResponse$;
  }

  buildExportUrl(id: string): string {
    return `/admin/export/${id}`;
  }
}

class FakeParksApiService {
  public searchResponse$: Observable<ParksApiResponse> = of({
    data: [createPark('legacy-park-1')],
    pagination: createPagination()
  });
  public parkByIdResponse$: Observable<Park> = of(createPark('legacy-park-by-id'));
  public readonly searchCalls: Array<{
    query: string;
    page: number;
    size: number;
    filters: ParkAdminListFilters | null;
    options: { closedFilter?: string; sort?: ParkAdminListSort };
  }> = [];
  public readonly getByIdCalls: string[] = [];

  searchParks(
    query: string,
    page: number,
    size: number,
    _visibleOnly: boolean = false,
    _region = null,
    filters: ParkAdminListFilters | null = null,
    options: { closedFilter?: string; sort?: ParkAdminListSort } = {}
  ): Observable<ParksApiResponse> {
    this.searchCalls.push({ query, page, size, filters, options });
    return this.searchResponse$;
  }

  getParkById(id: string): Observable<Park> {
    this.getByIdCalls.push(id);
    return this.parkByIdResponse$;
  }
}

function createPagination(): PaginationContract {
  return {
    currentPage: 1,
    itemsPerPage: 10,
    totalItems: 1,
    totalPages: 1
  };
}

function createAttraction(id: string | null = null): StandaloneAttraction {
  return {
    id,
    name: 'Bardonecchia Alpine Coaster',
    countryCode: 'IT',
    type: 'RollerCoaster',
    subtype: null,
    operatorId: null,
    websiteUrl: null,
    street: null,
    city: null,
    postalCode: null,
    latitude: null,
    longitude: null,
    descriptions: [],
    attractionDetails: {},
    attractionLocations: null,
    isVisible: false,
    adminReviewStatus: 'ToReview',
    legacyParkId: null,
    legacyParkItemId: null
  };
}

function createPark(id: string): Park {
  return {
    id,
    name: 'Bardonecchia Alpine Coaster',
    countryCode: 'IT',
    type: 'ThemePark',
    latitude: 45.07,
    longitude: 6.7,
    isVisible: false,
    adminReviewStatus: 'ToReview',
    city: 'Bardonecchia',
    parkItemsTotalCount: 1,
    parkItemsVisibleCount: 0,
    descriptions: []
  };
}

describe('AdminStandaloneAttractionsComponent', () => {
  let fixture: ComponentFixture<AdminStandaloneAttractionsComponent>;
  let component: AdminStandaloneAttractionsComponent;
  let standaloneApiService: FakeStandaloneAttractionsApiService;
  let parksApiService: FakeParksApiService;

  beforeEach(async () => {
    standaloneApiService = new FakeStandaloneAttractionsApiService();
    parksApiService = new FakeParksApiService();

    await TestBed.configureTestingModule({
      imports: [AdminStandaloneAttractionsComponent],
      providers: [
        { provide: StandaloneAttractionsApiService, useValue: standaloneApiService },
        { provide: ParksApiService, useValue: parksApiService },
        { provide: Router, useValue: { url: '/fr/admin/standalone-attractions' } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminStandaloneAttractionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('searches legacy parks from the standalone filters and fills the migration park id', async () => {
    const componentAccessor = component as unknown as {
      search: string;
      countryCode: string;
      migrationLegacyParkId: string;
      selectedLegacyPark: () => Park | null;
      searchLegacyParksFromFilters: () => Promise<void>;
    };

    componentAccessor.search = 'Bardonecchia';
    componentAccessor.countryCode = 'it';

    await componentAccessor.searchLegacyParksFromFilters();

    expect(parksApiService.searchCalls.length).toBe(1);
    expect(parksApiService.searchCalls[0].query).toBe('Bardonecchia');
    expect(parksApiService.searchCalls[0].page).toBe(1);
    expect(parksApiService.searchCalls[0].size).toBe(10);
    expect(parksApiService.searchCalls[0].filters?.countryCode).toBe('IT');
    expect(parksApiService.searchCalls[0].options.closedFilter).toBe('all');
    expect(componentAccessor.migrationLegacyParkId).toBe('legacy-park-1');
    expect(componentAccessor.selectedLegacyPark()?.id).toBe('legacy-park-1');
  });

  it('uses the selected standalone attraction as migration target when available', () => {
    const componentAccessor = component as unknown as {
      draft: { set: (value: StandaloneAttraction) => void };
      migrationTargetStandaloneAttractionId: string;
      migrationLegacyParkId: string;
      selectLegacyParkForMigration: (park: Park) => void;
    };

    componentAccessor.migrationTargetStandaloneAttractionId = 'stale-target';
    componentAccessor.draft.set(createAttraction('standalone-target'));
    componentAccessor.selectLegacyParkForMigration(createPark('legacy-park-2'));

    expect(componentAccessor.migrationLegacyParkId).toBe('legacy-park-2');
    expect(componentAccessor.migrationTargetStandaloneAttractionId).toBe('standalone-target');
  });

  it('shows the API problem detail when migration fails', async () => {
    const componentAccessor = component as unknown as {
      error: () => string | null;
      migrationLegacyParkId: string;
      migrateFromPark: () => Promise<void>;
    };
    standaloneApiService.migrateResponse$ = throwError(() => new HttpErrorResponse({
      status: 400,
      error: {
        status: 400,
        title: 'Migration impossible',
        detail: 'Le parc legacy contient plusieurs attractions.'
      }
    }));

    componentAccessor.migrationLegacyParkId = 'legacy-park-1';

    await componentAccessor.migrateFromPark();

    expect(componentAccessor.error()).toBe('Le parc legacy contient plusieurs attractions.');
  });
});
