import { HttpResponse } from '@angular/common/http';

import { Observable, of } from 'rxjs';

import { StandaloneAttraction, StandaloneAttractionMigrationRequest } from '@app/models/standalone-attractions/standalone-attraction';

import { PagedResult, PaginationContract } from '@shared/models/contracts';

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

export class FakeStandaloneAttractionsApiService {
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
