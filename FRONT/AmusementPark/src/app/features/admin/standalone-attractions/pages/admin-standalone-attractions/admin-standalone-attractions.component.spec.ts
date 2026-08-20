import {
  HttpErrorResponse,
  HttpHeaders,
  HttpResponse,
} from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import {
  ParkGraphUpsertRequest,
  ParkGraphUpsertResult,
} from '@app/models/admin/park-graph-upsert.models';
import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import {
  StandaloneAttraction,
  StandaloneAttractionMigrationRequest,
} from '@app/models/standalone-attractions/standalone-attraction';
import {
  ParkAdminListFilters,
  ParkAdminListSort,
} from '@data-access/parks/parks-api-endpoints';
import { PagedResult, PaginationContract } from '@shared/models/contracts';
import { ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT } from '../../../park-graph-upserts/state/admin-park-graph-upsert-operations.ports';
import {
  ADMIN_STANDALONE_ATTRACTIONS_DATA_PORT,
  ADMIN_STANDALONE_ATTRACTIONS_PARKS_PORT,
} from '../../state/admin-standalone-attractions.ports';

import { AdminStandaloneAttractionsComponent } from './admin-standalone-attractions.component';

class FakeStandaloneAttractionsApiService {
  public pageResponse$: Observable<PagedResult<StandaloneAttraction>> = of({
    items: [],
    pagination: createPagination(),
  });
  public migrateResponse$: Observable<StandaloneAttraction> = of(
    createAttraction('standalone-1'),
  );
  public exportResponse$: Observable<HttpResponse<Blob>> = of(
    new HttpResponse({
      body: new Blob(['{}'], { type: 'application/json' }),
    }),
  );
  public readonly pageCalls: Array<{
    page: number;
    size: number;
    filters: unknown;
  }> = [];
  public readonly migrationCalls: StandaloneAttractionMigrationRequest[] = [];
  public readonly exportCalls: string[] = [];
  public readonly getByIdCalls: string[] = [];

  getAdminPage(
    page: number,
    size: number,
    filters: unknown,
  ): Observable<PagedResult<StandaloneAttraction>> {
    this.pageCalls.push({ page, size, filters });
    return this.pageResponse$;
  }

  getAdminById(id: string): Observable<StandaloneAttraction> {
    this.getByIdCalls.push(id);
    return of(createAttraction(id));
  }

  create(attraction: StandaloneAttraction): Observable<StandaloneAttraction> {
    return of({ ...attraction, id: 'created-standalone' });
  }

  update(
    _id: string,
    attraction: StandaloneAttraction,
  ): Observable<StandaloneAttraction> {
    return of(attraction);
  }

  updateBulkAdministration(): Observable<{
    requestedCount: number;
    updatedCount: number;
  }> {
    return of({ requestedCount: 0, updatedCount: 0 });
  }

  migrateFromPark(
    request: StandaloneAttractionMigrationRequest,
  ): Observable<StandaloneAttraction> {
    this.migrationCalls.push(request);
    return this.migrateResponse$;
  }

  downloadExport(id: string): Observable<HttpResponse<Blob>> {
    this.exportCalls.push(id);
    return this.exportResponse$;
  }
}

class FakeParkGraphUpsertsApiService {
  public previewResponse$: Observable<ParkGraphUpsertResult> = of(
    createUpsertResult(false, true),
  );
  public applyResponse$: Observable<ParkGraphUpsertResult> = of(
    createUpsertResult(true, false),
  );
  public readonly previewCalls: ParkGraphUpsertRequest[] = [];
  public readonly applyCalls: ParkGraphUpsertRequest[] = [];

  preview(request: ParkGraphUpsertRequest): Observable<ParkGraphUpsertResult> {
    this.previewCalls.push(request);
    return this.previewResponse$;
  }

  apply(request: ParkGraphUpsertRequest): Observable<ParkGraphUpsertResult> {
    this.applyCalls.push(request);
    return this.applyResponse$;
  }
}

class FakeParksApiService {
  public searchResponse$: Observable<ParksApiResponse> = of({
    data: [createPark('legacy-park-1')],
    pagination: createPagination(),
  });
  public parkByIdResponse$: Observable<Park> = of(
    createPark('legacy-park-by-id'),
  );
  public readonly searchCalls: Array<{
    query: string;
    page: number;
    size: number;
    filters: ParkAdminListFilters | null;
    options: {
      closedFilter?: string;
      sort?: ParkAdminListSort;
    };
  }> = [];
  public readonly getByIdCalls: string[] = [];

  searchParks(
    query: string,
    page: number,
    size: number,
    _visibleOnly: boolean = false,
    _region = null,
    filters: ParkAdminListFilters | null = null,
    options: {
      closedFilter?: string;
      sort?: ParkAdminListSort;
    } = {},
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
    totalPages: 1,
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
    legacyParkItemId: null,
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
    descriptions: [],
  };
}

function createUpsertResult(
  isApplied: boolean,
  canApply: boolean,
): ParkGraphUpsertResult {
  return {
    operationId: 'operation-1',
    mode: isApplied ? 'Apply' : 'Preview',
    isApplied,
    canApply,
    previewedAtUtc: '2026-08-20T10:00:00Z',
    appliedAtUtc: isApplied ? '2026-08-20T10:01:00Z' : null,
    targetStandaloneAttractionId: 'standalone-1',
    targetStandaloneAttractionName: 'Bardonecchia Alpine Coaster',
    counts: {
      created: 0,
      updated: 1,
      deleted: 0,
      unchanged: 0,
      warnings: 0,
      errors: 0,
    },
    changes: [],
    warnings: [],
    errors: [],
  };
}

describe('AdminStandaloneAttractionsComponent', () => {
  let fixture: ComponentFixture<AdminStandaloneAttractionsComponent>;
  let component: AdminStandaloneAttractionsComponent;
  let standaloneApiService: FakeStandaloneAttractionsApiService;
  let parksApiService: FakeParksApiService;
  let parkGraphUpsertsApiService: FakeParkGraphUpsertsApiService;

  beforeEach(async () => {
    standaloneApiService = new FakeStandaloneAttractionsApiService();
    parksApiService = new FakeParksApiService();
    parkGraphUpsertsApiService = new FakeParkGraphUpsertsApiService();

    await TestBed.configureTestingModule({
      imports: [AdminStandaloneAttractionsComponent],
      providers: [
        {
          provide: ADMIN_STANDALONE_ATTRACTIONS_DATA_PORT,
          useValue: standaloneApiService,
        },
        {
          provide: ADMIN_STANDALONE_ATTRACTIONS_PARKS_PORT,
          useValue: parksApiService,
        },
        {
          provide: ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT,
          useValue: parkGraphUpsertsApiService,
        },
        {
          provide: Router,
          useValue: { url: '/fr/admin/standalone-attractions' },
        },
      ],
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

  it('creates a new draft from the active filters', () => {
    const componentAccessor = component as unknown as {
      search: string;
      countryCode: string;
      typeFilter: string;
      draft: () => StandaloneAttraction;
      newAttraction: () => void;
    };

    componentAccessor.search = 'Bardonecchia';
    componentAccessor.countryCode = 'it';
    componentAccessor.typeFilter = 'RollerCoaster';

    componentAccessor.newAttraction();

    expect(componentAccessor.draft().name).toBe('Bardonecchia');
    expect(componentAccessor.draft().countryCode).toBe('IT');
    expect(componentAccessor.draft().type).toBe('RollerCoaster');
  });

  it('keeps the saved attraction reachable when filters would hide it', async () => {
    const componentAccessor = component as unknown as {
      search: string;
      countryCode: string;
      typeFilter: string;
      isVisibleFilter: string;
      reviewStatusFilter: string;
      draft: {
        set: (value: StandaloneAttraction) => void;
      };
      selected: () => StandaloneAttraction | null;
      saveDraft: () => Promise<void>;
    };

    componentAccessor.search = 'Legacy name';
    componentAccessor.countryCode = 'fr';
    componentAccessor.typeFilter = 'WaterRide';
    componentAccessor.isVisibleFilter = 'true';
    componentAccessor.reviewStatusFilter = 'Validated';
    componentAccessor.draft.set({
      ...createAttraction(null),
      name: 'Bardonecchia Alpine Coaster',
      countryCode: 'IT',
      type: 'RollerCoaster',
      isVisible: false,
      adminReviewStatus: 'ToReview',
    });

    await componentAccessor.saveDraft();

    const lastPageCall = standaloneApiService.pageCalls[
      standaloneApiService.pageCalls.length - 1
    ] as {
      page: number;
      filters: {
        search?: string | null;
        countryCode?: string | null;
        type?: string | null;
        isVisible?: boolean | null;
        adminReviewStatus?: string | null;
        sortBy?: string | null;
        sortDirection?: string | null;
      };
    };

    expect(componentAccessor.selected()?.id).toBe('created-standalone');
    expect(lastPageCall.page).toBe(1);
    expect(lastPageCall.filters.search).toBe('Bardonecchia Alpine Coaster');
    expect(lastPageCall.filters.countryCode).toBe('IT');
    expect(lastPageCall.filters.type).toBe('RollerCoaster');
    expect(lastPageCall.filters.isVisible).toBeNull();
    expect(lastPageCall.filters.adminReviewStatus).toBe('ToReview');
    expect(lastPageCall.filters.sortBy).toBe('updated');
    expect(lastPageCall.filters.sortDirection).toBe('desc');
  });

  it('uses the selected standalone attraction as migration target when available', () => {
    const componentAccessor = component as unknown as {
      draft: {
        set: (value: StandaloneAttraction) => void;
      };
      migrationTargetStandaloneAttractionId: string;
      migrationLegacyParkId: string;
      selectLegacyParkForMigration: (park: Park) => void;
    };

    componentAccessor.migrationTargetStandaloneAttractionId = 'stale-target';
    componentAccessor.draft.set(createAttraction('standalone-target'));
    componentAccessor.selectLegacyParkForMigration(createPark('legacy-park-2'));

    expect(componentAccessor.migrationLegacyParkId).toBe('legacy-park-2');
    expect(componentAccessor.migrationTargetStandaloneAttractionId).toBe(
      'standalone-target',
    );
  });

  it('shows the API problem detail when migration fails', async () => {
    const componentAccessor = component as unknown as {
      error: () => string | null;
      migrationLegacyParkId: string;
      migrateFromPark: () => Promise<void>;
    };
    standaloneApiService.migrateResponse$ = throwError(
      () =>
        new HttpErrorResponse({
          status: 400,
          error: {
            status: 400,
            title: 'Migration impossible',
            detail: 'Le parc legacy contient plusieurs attractions.',
          },
        }),
    );

    componentAccessor.migrationLegacyParkId = 'legacy-park-1';

    await componentAccessor.migrateFromPark();

    expect(componentAccessor.error()).toBe(
      'Le parc legacy contient plusieurs attractions.',
    );
  });

  it('downloads standalone JSON exports through the authenticated API service', async () => {
    const responseBlob: Blob = new Blob(['{}'], { type: 'application/json' });
    const createObjectUrlSpy = vi
      .spyOn(URL, 'createObjectURL')
      .mockReturnValue('blob:standalone-export');
    const revokeObjectUrlSpy = vi
      .spyOn(URL, 'revokeObjectURL')
      .mockImplementation(() => {});
    const clickSpy = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => {});
    standaloneApiService.exportResponse$ = of(
      new HttpResponse({
        body: responseBlob,
        headers: new HttpHeaders({
          'content-disposition':
            'attachment; filename="standalone-export.json"',
        }),
      }),
    );
    const componentAccessor = component as unknown as {
      draft: {
        set: (value: StandaloneAttraction) => void;
      };
      exportDraft: () => Promise<void>;
      error: () => string | null;
    };

    componentAccessor.draft.set(createAttraction('standalone-1'));

    await componentAccessor.exportDraft();

    expect(standaloneApiService.exportCalls).toEqual(['standalone-1']);
    expect(createObjectUrlSpy).toHaveBeenCalledTimes(1);
    expect(createObjectUrlSpy).toHaveBeenCalledWith(responseBlob);
    expect(clickSpy).toHaveBeenCalled();
    expect(revokeObjectUrlSpy).toHaveBeenCalledTimes(1);
    expect(revokeObjectUrlSpy).toHaveBeenCalledWith('blob:standalone-export');
    expect(componentAccessor.error()).toBeNull();
  });

  it('previews a standalone JSON import through the graph upsert facade', async () => {
    const componentAccessor = component as unknown as {
      previewImportJson: (jsonText: string, fileName: string) => Promise<void>;
      importPreviewResult: () => ParkGraphUpsertResult | null;
      error: () => string | null;
    };
    const document = {
      documentType: 'standaloneAttractionGraph',
      standaloneAttraction: {
        id: 'standalone-1',
        name: 'Bardonecchia Alpine Coaster',
      },
    };

    await componentAccessor.previewImportJson(
      JSON.stringify(document),
      'standalone-export.json',
    );

    expect(parkGraphUpsertsApiService.previewCalls).toEqual([
      {
        targetParkId: null,
        createIfMissing: false,
        replaceCollections: false,
        document,
      },
    ]);
    expect(componentAccessor.importPreviewResult()?.canApply).toBe(true);
    expect(componentAccessor.error()).toBeNull();
  });

  it('rejects a park graph on the standalone import control', async () => {
    const componentAccessor = component as unknown as {
      previewImportJson: (jsonText: string, fileName: string) => Promise<void>;
      error: () => string | null;
    };

    await componentAccessor.previewImportJson(
      JSON.stringify({
        documentType: 'AmusementParkParkGraphUpsert',
        park: { id: 'park-1' },
        standaloneAttraction: { id: 'standalone-1' },
      }),
      'park-export.json',
    );

    expect(parkGraphUpsertsApiService.previewCalls).toEqual([]);
    expect(componentAccessor.error()).toBe(
      'Ce fichier ne décrit pas une attraction isolée.',
    );
  });

  it('applies only a successful preview and reloads the imported attraction', async () => {
    const componentAccessor = component as unknown as {
      previewImportJson: (jsonText: string, fileName: string) => Promise<void>;
      applyImport: () => Promise<void>;
      selected: () => StandaloneAttraction | null;
      message: () => string | null;
    };
    const document = {
      documentType: 'standaloneAttractionGraph',
      standaloneAttraction: { id: 'standalone-1' },
    };

    await componentAccessor.previewImportJson(
      JSON.stringify(document),
      'standalone-export.json',
    );
    await componentAccessor.applyImport();

    expect(parkGraphUpsertsApiService.applyCalls).toEqual(
      parkGraphUpsertsApiService.previewCalls,
    );
    expect(standaloneApiService.getByIdCalls).toEqual(['standalone-1']);
    expect(componentAccessor.selected()?.id).toBe('standalone-1');
    expect(componentAccessor.message()).toBe('Import JSON appliqué.');
  });
});
