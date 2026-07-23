import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { ParkGraphUpsertsApiService } from '@data-access/admin/park-graph-upserts-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { AdminParkGraphUpsertOperationsFacade } from './admin-park-graph-upsert-operations.facade';

describe('AdminParkGraphUpsertOperationsFacade', () => {
  let facade: AdminParkGraphUpsertOperationsFacade;
  let parksApi: jasmine.SpyObj<ParksApiService>;
  let graphApi: jasmine.SpyObj<ParkGraphUpsertsApiService>;

  beforeEach(() => {
    parksApi = jasmine.createSpyObj<ParksApiService>('ParksApiService', ['searchParks', 'getParkDataCompletenessScore']);
    graphApi = jasmine.createSpyObj<ParkGraphUpsertsApiService>('ParkGraphUpsertsApiService', ['downloadParkExport', 'preview', 'apply']);

    TestBed.configureTestingModule({
      providers: [
        AdminParkGraphUpsertOperationsFacade,
        { provide: ParksApiService, useValue: parksApi },
        { provide: ParkGraphUpsertsApiService, useValue: graphApi }
      ]
    });
    facade = TestBed.inject(AdminParkGraphUpsertOperationsFacade);
  });

  it('keeps the admin park search contract in one orchestration boundary', () => {
    const response = {
      data: [],
      pagination: { totalItems: 0, currentPage: 1, itemsPerPage: 10, totalPages: 0 }
    };
    parksApi.searchParks.and.returnValue(of(response));

    facade.searchParks('Europa').subscribe();

    expect(parksApi.searchParks).toHaveBeenCalledOnceWith('Europa', 1, 10, false, null, null);
  });

  it('delegates preview and apply without changing their request', () => {
    const request: ParkGraphUpsertRequest = {
      createIfMissing: false,
      replaceCollections: false,
      document: {}
    };
    const result = { canApply: true } as ParkGraphUpsertResult;
    graphApi.preview.and.returnValue(of(result));
    graphApi.apply.and.returnValue(of(result));

    facade.preview(request).subscribe();
    facade.apply(request).subscribe();

    expect(graphApi.preview).toHaveBeenCalledOnceWith(request);
    expect(graphApi.apply).toHaveBeenCalledOnceWith(request);
  });
});
