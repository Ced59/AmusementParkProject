import { HttpResponse } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { BulkAdministrationUpdateRequest, BulkAdministrationUpdateResult } from '@app/models/admin/admin-review-status';
import { ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { Park } from '@app/models/parks/park';
import { StandaloneAttraction, StandaloneAttractionMigrationRequest } from '@app/models/standalone-attractions/standalone-attraction';
import { PagedResult } from '@shared/models/contracts';
import {
  ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT,
  AdminParkGraphUpsertGraphPort
} from '../../park-graph-upserts/state/admin-park-graph-upsert-operations.ports';
import {
  ADMIN_STANDALONE_ATTRACTIONS_DATA_PORT,
  ADMIN_STANDALONE_ATTRACTIONS_PARKS_PORT,
  AdminStandaloneAttractionListFilters,
  AdminStandaloneAttractionsDataPort,
  AdminStandaloneAttractionsParksPort,
  AdminStandaloneLegacyParkFilters
} from './admin-standalone-attractions.ports';

@Injectable({
  providedIn: 'root'
})
export class AdminStandaloneAttractionsFacade {
  constructor(
    @Inject(ADMIN_STANDALONE_ATTRACTIONS_DATA_PORT) private readonly standaloneAttractions: AdminStandaloneAttractionsDataPort,
    @Inject(ADMIN_STANDALONE_ATTRACTIONS_PARKS_PORT) private readonly parks: AdminStandaloneAttractionsParksPort,
    @Inject(ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT) private readonly graphUpserts: AdminParkGraphUpsertGraphPort
  ) {
  }

  loadPage(page: number, size: number, filters: AdminStandaloneAttractionListFilters): Observable<PagedResult<StandaloneAttraction>> {
    return this.standaloneAttractions.getAdminPage(page, size, filters);
  }

  loadById(id: string): Observable<StandaloneAttraction> {
    return this.standaloneAttractions.getAdminById(id);
  }

  create(attraction: StandaloneAttraction): Observable<StandaloneAttraction> {
    return this.standaloneAttractions.create(attraction);
  }

  update(id: string, attraction: StandaloneAttraction): Observable<StandaloneAttraction> {
    return this.standaloneAttractions.update(id, attraction);
  }

  updateBulkAdministration(request: BulkAdministrationUpdateRequest): Observable<BulkAdministrationUpdateResult> {
    return this.standaloneAttractions.updateBulkAdministration(request);
  }

  migrateFromPark(request: StandaloneAttractionMigrationRequest): Observable<StandaloneAttraction> {
    return this.standaloneAttractions.migrateFromPark(request);
  }

  downloadExport(id: string): Observable<HttpResponse<Blob>> {
    return this.standaloneAttractions.downloadExport(id);
  }

  loadLegacyParks(query: string, filters: AdminStandaloneLegacyParkFilters | null): Observable<Park[]> {
    if (this.isIdentifier(query)) {
      return this.parks.getParkById(query).pipe(
        map((park: Park): Park[] => [park])
      );
    }

    return this.parks.searchParks(
      query,
      1,
      10,
      false,
      null,
      filters,
      {
        closedFilter: 'all',
        sort: { sortBy: 'name', sortDirection: 'asc' }
      }
    ).pipe(
      map((response): Park[] => response.data ?? [])
    );
  }

  previewImport(request: ParkGraphUpsertRequest): Observable<ParkGraphUpsertResult> {
    return this.graphUpserts.preview(request);
  }

  applyImport(request: ParkGraphUpsertRequest): Observable<ParkGraphUpsertResult> {
    return this.graphUpserts.apply(request);
  }

  private isIdentifier(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value.trim());
  }
}
