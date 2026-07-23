import { HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { DataCompletenessScore } from '@app/models/shared/data-completeness-score';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ParkGraphUpsertsApiService } from '@data-access/admin/park-graph-upserts-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';

@Injectable({
  providedIn: 'root'
})
export class AdminParkGraphUpsertOperationsFacade {
  constructor(
    private readonly parksApi: ParksApiService,
    private readonly parkGraphUpsertsApi: ParkGraphUpsertsApiService
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
