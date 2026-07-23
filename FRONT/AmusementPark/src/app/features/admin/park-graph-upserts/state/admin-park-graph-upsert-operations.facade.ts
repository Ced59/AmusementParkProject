import { HttpResponse } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { DataCompletenessScore } from '@app/models/shared/data-completeness-score';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import {
  ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT,
  ADMIN_PARK_GRAPH_UPSERT_PARKS_PORT,
  AdminParkGraphUpsertGraphPort,
  AdminParkGraphUpsertParksPort
} from './admin-park-graph-upsert-operations.ports';

@Injectable({
  providedIn: 'root'
})
export class AdminParkGraphUpsertOperationsFacade {
  constructor(
    @Inject(ADMIN_PARK_GRAPH_UPSERT_PARKS_PORT) private readonly parksApi: AdminParkGraphUpsertParksPort,
    @Inject(ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT) private readonly parkGraphUpsertsApi: AdminParkGraphUpsertGraphPort
  ) {
  }

  searchParks(query: string): Observable<ParksApiResponse> {
    return this.parksApi.searchParks(query, 1, 10, false, null, null);
  }

  loadParkDataCompleteness(parkId: string): Observable<DataCompletenessScore> {
    return this.parksApi.getParkDataCompletenessScore(parkId);
  }

  downloadParkExport(parkId: string): Observable<HttpResponse<Blob>> {
    return this.parkGraphUpsertsApi.downloadParkExport(parkId);
  }

  preview(request: ParkGraphUpsertRequest): Observable<ParkGraphUpsertResult> {
    return this.parkGraphUpsertsApi.preview(request);
  }

  apply(request: ParkGraphUpsertRequest): Observable<ParkGraphUpsertResult> {
    return this.parkGraphUpsertsApi.apply(request);
  }
}
